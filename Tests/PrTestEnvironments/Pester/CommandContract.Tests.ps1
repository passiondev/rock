<#
    The queue agent's verb table, and the binder that turns a queued JSON document
    into the arguments one deployment script is called with.

    Until 2026-09-15 both were a switch inside the scriptblock handed to Start-Job.
    Start-Job runs its block in a separate runspace, so nothing in this repository
    could call it: what it did was asserted by matching the text of its own source,
    and a source match can only ever report that the text is still there. Adding a
    verb took four edits in four places -- a timeout table, the switch arm, the
    secret list and the producer's payload -- and nothing asserted that the four
    agreed.

    What that shape hid, found while writing these: the payload-to-argument step was
    written out five times, the comma-split list parser twice word for word, and
    three different rules governed a blank value with nothing saying which was meant
    where. `find-legacy-text-columns` checked its connection string for presence and
    `anonymize-staging` checked its catalog for presence and non-blankness, in
    adjacent arms, both reading as deliberate.

    So these tests call it. The last Describe is the one the old shape could not
    even express: it takes every parameter every contract binds, puts them through
    the same serializer Start-Job puts them through, and splats them at the real
    signature of the script that contract names.
#>

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'ScriptFunctions.psm1') -Force

    $script:AgentScript = Get-RepositoryPath 'Deployment/PrTestEnvironments/Invoke-PrEnvironmentCommandQueue.ps1'
    . (Import-ScriptFunction -Path $script:AgentScript -Name `
            'Get-CommandBindingKind', 'Get-CommandContractSection', 'Get-CommandContract', `
            'Split-CommandList', 'Resolve-CommandArguments')

    # Both directories, because the bootstrap copies them into one directory on the
    # box: a contract names a bare file name and the agent joins it onto $DeployRoot,
    # so a script's folder in this repository is not where the agent looks for it.
    $script:ScriptRoots = @(
        (Get-RepositoryPath 'Deployment/PrTestEnvironments'),
        (Get-RepositoryPath 'Deployment/Database')
    )

    function script:Find-ContractScript {
        param([Parameter(Mandatory = $true)][string]$Name)

        foreach ($root in $script:ScriptRoots) {
            $candidate = Join-Path $root $Name
            if (Test-Path -Path $candidate -PathType Leaf) { return $candidate }
        }
        return $null
    }

    function script:Get-ScriptParameter {
        <#
            Name -> declared type for one script's parameters, read from the parser
            rather than from a hand-kept list, so this tracks a rename by failing.
        #>
        param([Parameter(Mandatory = $true)][string]$Path)

        $errors = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseFile($Path, [ref]$null, [ref]$errors)
        if (@($errors).Count -gt 0) { throw "$Path does not parse: $($errors[0].Message)" }

        $parameters = [ordered]@{}
        foreach ($parameter in $ast.ParamBlock.Parameters) {
            $parameters[$parameter.Name.VariablePath.UserPath] = $parameter.StaticType
        }
        return $parameters
    }

    function script:New-SignatureProbe {
        <#
            The script's parameter list with its bodies and defaults dropped, so that
            splatting at it exercises the real names and the real types without
            deploying anything. -Rename misspells one parameter, which is how the
            sweep below proves it can still fail.
        #>
        param(
            [Parameter(Mandatory = $true)][string]$Path,
            [Parameter(Mandatory = $false)][string]$Rename
        )

        $declarations = foreach ($entry in (script:Get-ScriptParameter -Path $Path).GetEnumerator()) {
            $name = if ($entry.Key -eq $Rename) { "Renamed$($entry.Key)" } else { $entry.Key }
            "[$($entry.Value.FullName)]`$$name"
        }
        # [CmdletBinding()] because without it a name that matches nothing is not an
        # error: PowerShell drops it into $args and the script runs on without it.
        # Every script these contracts name declares it, and a name that binds
        # nowhere is what this is looking for.
        return [scriptblock]::Create(
            "[CmdletBinding()]param($($declarations -join ', '))`n`$PSBoundParameters")
    }

    function script:New-MaximalCommand {
        <#
            A queued document stating every field one contract knows about, so the
            sweep covers a verb's whole surface without a fixture per verb -- which
            is how a field added to a contract gets covered by writing nothing.

            Values are typed off the target parameter, because prNumber reaches an
            [int] and "value-for-prNumber" would fail as a conversion rather than as
            the name mismatch this is looking for.
        #>
        param([Parameter(Mandatory = $true)][string]$Verb)

        $contract = Get-CommandContract -Command $Verb
        $types = if ($contract.Contains('Script')) {
            $path = script:Find-ContractScript -Name ([string]$contract.Script)
            if ($path) { script:Get-ScriptParameter -Path $path } else { [ordered]@{} }
        }
        else { [ordered]@{} }

        $document = [ordered]@{ command = $Verb }

        foreach ($kind in @('Required', 'Optional', 'Verbatim')) {
            if (-not $contract.Contains($kind)) { continue }
            foreach ($field in $contract[$kind].Keys) {
                $parameter = $contract[$kind][$field]
                $numeric = $types.Contains($parameter) -and
                    $types[$parameter].FullName -match '^System\.(Int16|Int32|Int64|Decimal|Double|Single)$'
                $document[$field] = if ($numeric) { '1234' } else { "value-for-$field" }
            }
        }
        if ($contract.Contains('Flag')) {
            foreach ($field in $contract.Flag.Keys) { $document[$field] = $true }
        }
        if ($contract.Contains('List')) {
            foreach ($field in $contract.List.Keys) { $document[$field] = 'first,second' }
        }

        return [pscustomobject]$document
    }

    function script:New-Command {
        # A queued document is JSON on the way in, so the tests build one the same
        # way: a hashtable through ConvertTo-Json arrives as the PSCustomObject the
        # binder actually sees, including the property-is-missing case that
        # Set-StrictMode turns into a terminating error.
        param([Parameter(Mandatory = $true)][hashtable]$Fields)

        return ($Fields | ConvertTo-Json -Depth 5 | ConvertFrom-Json)
    }

    $script:DeployRoot = 'C:\RockDeploy'
    $script:StepLog = 'C:\RockDeploy\logs\deploy-staging-1-steps.log'
}

Describe 'Get-CommandContract' {

    It 'refuses a verb it has no row for, by name' {
        # The message a workflow sees after its poll. "Unknown command: teleport"
        # sends somebody to this table; anything vaguer sends them to the queue.
        { Get-CommandContract -Command 'teleport' } | Should -Throw -ExpectedMessage '*Unknown command: teleport*'
    }

    It 'hands back the whole table when asked for no verb in particular' {
        $table = Get-CommandContract

        @($table.Keys).Count | Should -BeGreaterThan 1
        $table.Contains('deploy-environment') | Should -BeTrue
    }

    It 'names a script that ships to the box for every verb' {
        $missing = foreach ($verb in (Get-CommandContract).Keys) {
            $contract = Get-CommandContract -Command $verb
            if (-not $contract.Contains('Script')) { "$verb declares no Script"; continue }
            if (-not (script:Find-ContractScript -Name ([string]$contract.Script))) {
                "$verb names $($contract.Script), which is in neither published directory"
            }
        }

        @($missing) -join '; ' | Should -BeNullOrEmpty
    }

    It 'declares a timeout for every verb rather than falling back to one' {
        # There is no fallback any more. The old one was 600 seconds and unreachable:
        # every verb was already in the timeout table, so the constant only stood by
        # to give a forgotten verb a quiet default that would kill a real run.
        $wrong = foreach ($verb in (Get-CommandContract).Keys) {
            $contract = Get-CommandContract -Command $verb
            if (-not $contract.Contains('TimeoutSeconds')) { "$verb declares no TimeoutSeconds"; continue }
            if ([int]$contract.TimeoutSeconds -le 0) { "$verb declares a timeout of $($contract.TimeoutSeconds)" }
        }

        @($wrong) -join '; ' | Should -BeNullOrEmpty
    }

    It 'uses only section names the binder knows how to bind' {
        # `Flags` where `Flag` was meant binds nothing and says nothing: -Apply
        # silently stops being forwarded and every queued run becomes a dry run that
        # reports success.
        #
        # Both lists are asked for. The non-binding names used to be written out
        # here as @('Script', 'TimeoutSeconds'), which is the same list the binder
        # carried, so adding Unreachable meant editing a test to keep it passing --
        # and a test that has to be edited to accept a new section is not checking
        # the section, it is following it.
        $kinds = Get-CommandBindingKind
        $sections = Get-CommandContractSection
        $strays = foreach ($verb in (Get-CommandContract).Keys) {
            foreach ($section in (Get-CommandContract -Command $verb).Keys) {
                if ($sections -contains $section) { continue }
                if ($kinds -notcontains $section) { "$verb declares '$section'" }
            }
        }

        @($strays) -join '; ' | Should -BeNullOrEmpty
    }

    It 'accounts for every parameter of the script each verb names' {
        # The half the table did not state. It listed what a queued document can
        # set and said nothing about the rest, so a parameter added to a deployment
        # script and never wired here was indistinguishable from one deliberately
        # left on its default -- and the difference is the whole question during an
        # incident, when somebody wants to move a value and has to work out from two
        # files whether they can.
        #
        # Deploy-RockEnvironment.ps1 takes seventeen and this reached thirteen. The
        # four are now Unreachable with reasons; what makes those reasons stay true
        # is that a new parameter fails here until somebody writes one.
        $unaccounted = foreach ($verb in (Get-CommandContract).Keys) {
            $contract = Get-CommandContract -Command $verb
            $path = script:Find-ContractScript -Name ([string]$contract.Script)
            if (-not $path) { continue }

            $bound = foreach ($kind in (Get-CommandBindingKind)) {
                if ($contract.Contains($kind)) { $contract[$kind].Values }
            }
            $stated = @($bound) + @(if ($contract.Contains('Unreachable')) { $contract.Unreachable })

            foreach ($parameter in (script:Get-ScriptParameter -Path $path).Keys) {
                if ($stated -notcontains $parameter) {
                    "$verb neither binds nor declares unreachable: $parameter"
                }
            }
        }

        @($unaccounted) -join '; ' | Should -BeNullOrEmpty `
            -Because 'a parameter a contract says nothing about is a decision nobody made'
    }

    It 'declares nothing unreachable that the script does not take' {
        # The other direction, and the one that rots quietly. A renamed parameter
        # leaves its old name sitting in Unreachable, where it goes on satisfying
        # the sweep above for a parameter that no longer exists while the real one
        # is unaccounted for -- except the real one would fail the sweep, so this
        # exists for the rename that removes a parameter outright and leaves the
        # reason behind as a claim about nothing.
        $stale = foreach ($verb in (Get-CommandContract).Keys) {
            $contract = Get-CommandContract -Command $verb
            if (-not $contract.Contains('Unreachable')) { continue }
            $path = script:Find-ContractScript -Name ([string]$contract.Script)
            if (-not $path) { continue }

            $parameters = @((script:Get-ScriptParameter -Path $path).Keys)
            foreach ($name in $contract.Unreachable) {
                if ($parameters -notcontains $name) {
                    "$verb holds $name out of reach, and $($contract.Script) has no such parameter"
                }
            }
        }

        @($stale) -join '; ' | Should -BeNullOrEmpty
    }

    It 'would notice a parameter nothing accounted for' {
        # Calibration. Both sweeps above report an empty list on a clean tree and
        # would report the same empty list if they had stopped reading either side.
        $contract = Get-CommandContract -Command 'deploy-environment'
        $bound = foreach ($kind in (Get-CommandBindingKind)) {
            if ($contract.Contains($kind)) { $contract[$kind].Values }
        }
        $stated = @($bound) + @($contract.Unreachable)

        # The parameter a deploy would add next, spelled as the script would spell
        # it. Neither list has it, and that is what a failure looks like.
        $stated | Should -Not -Contain 'RollbackRoot'
        # And the two lists really are reading the file: these are the ends of the
        # signature, one from each side of the question.
        $stated | Should -Contain 'EnvironmentName'
        $stated | Should -Contain 'HealthCheckTimeoutSeconds'
    }
}

Describe 'Split-CommandList' {

    It 'splits a comma-separated field into its values' {
        @(Split-CommandList -Value 'a.com,b.org') | Should -Be @('a.com', 'b.org')
    }

    It 'trims, because a hand-written command is written with spaces after commas' {
        @(Split-CommandList -Value ' a.com , b.org ') | Should -Be @('a.com', 'b.org')
    }

    It 'drops a blank between two commas rather than passing an empty domain on' {
        @(Split-CommandList -Value 'a.com,,b.org') | Should -Be @('a.com', 'b.org')
    }

    It 'yields nothing for an empty value' {
        @(Split-CommandList -Value '').Count | Should -Be 0
    }

    It 'yields nothing for a value that is all separators and space' {
        # This is what lets the caller read "nothing survived" as "the field was not
        # stated", which for the anonymizer's keep list means anonymize everyone --
        # the stricter of the two readings.
        @(Split-CommandList -Value ' , , ').Count | Should -Be 0
    }
}

Describe 'Resolve-CommandArguments' {

    Context 'the verbs that take a PR number' {

        It 'binds a deploy at the five fields its script requires' {
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command                 = 'deploy'
                    prNumber                = 1234
                    sha                     = 'abc123'
                    artifactGcsPath         = 'gs://bucket/artifact.zip'
                    hostName                = 'pr-1234.example.test'
                    sandboxConnectionString = 'Server=x;Database=y'
                })

            $plan.Script | Should -Be 'Deploy-PrEnvironment.ps1'
            $plan.Arguments['PrNumber'] | Should -Be '1234'
            $plan.Arguments['Sha'] | Should -Be 'abc123'
            $plan.Arguments['SandboxConnectionString'] | Should -Be 'Server=x;Database=y'
        }

        It 'binds stop and destroy at the PR number alone' {
            foreach ($verb in @('stop', 'destroy')) {
                $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                        command = $verb; prNumber = 7
                    })

                @($plan.Arguments.Keys) | Should -Be @('PrNumber')
            }
        }

        It 'refuses a deploy missing a field instead of calling the script without it' {
            { Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                        command                 = 'deploy'
                        prNumber                = 1234
                        artifactGcsPath         = 'gs://bucket/artifact.zip'
                        hostName                = 'pr-1234.example.test'
                        sandboxConnectionString = 'Server=x;Database=y'
                    }) } | Should -Throw -ExpectedMessage '*deploy requires sha*'
        }
    }

    Context 'deploy-environment' {

        It 'binds the four fields every named environment states' {
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -StepLogPath $script:StepLog -Command (script:New-Command @{
                    command         = 'deploy-environment'
                    environmentName = 'staging'
                    sha             = 'abc123'
                    artifactGcsPath = 'gs://bucket/artifact.zip'
                    hostName        = 'staging.example.test'
                })

            $plan.Script | Should -Be 'Deploy-RockEnvironment.ps1'
            $plan.Arguments['EnvironmentName'] | Should -Be 'staging'
            $plan.Arguments['HostName'] | Should -Be 'staging.example.test'
        }

        It 'leaves out a connection string the command did not state' {
            # Production omits it so the box keeps the one already on disk. Passing an
            # empty string instead would overwrite it with nothing, which is the whole
            # reason this field is Optional rather than Required.
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command          = 'deploy-environment'
                    environmentName  = 'production'
                    sha              = 'abc123'
                    artifactGcsPath  = 'gs://bucket/artifact.zip'
                    hostName         = 'connect.example.test'
                    connectionString = ''
                })

            $plan.Arguments.ContainsKey('ConnectionString') | Should -BeFalse
        }

        It 'forwards the site, pool and root overrides when the command states them' {
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command           = 'deploy-environment'
                    environmentName   = 'production'
                    sha               = 'abc123'
                    artifactGcsPath   = 'gs://bucket/artifact.zip'
                    hostName          = 'connect.example.test'
                    mode              = 'InPlace'
                    targetSitePath    = 'C:\inetpub\Rock'
                    targetSiteName    = 'Rock'
                    targetAppPoolName = 'RockProdPool'
                    environmentRoot   = 'C:\RockEnvs'
                })

            $plan.Arguments['Mode'] | Should -Be 'InPlace'
            $plan.Arguments['TargetAppPoolName'] | Should -Be 'RockProdPool'
            $plan.Arguments['EnvironmentRoot'] | Should -Be 'C:\RockEnvs'
        }

        It 'lets a hand-written command move where the rollback copy lands' {
            # The sharp one. On InPlace this is where production's only rollback
            # goes, and where the manifest goes with it -- never under
            # $EnvironmentRoot, which the certificate renewal job walks. It was the
            # one parameter with that much riding on it that nothing could state,
            # which made a full disk on the morning of a cutover an edit to a script
            # on the box rather than a field in a command.
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command = 'deploy-environment'; environmentName = 'production'; sha = 'a'
                    artifactGcsPath = 'g'; hostName = 'h'; mode = 'InPlace'
                    targetSitePath = 'C:\inetpub\Rock'; backupRoot = 'D:\RockBackups'
                })

            $plan.Arguments['BackupRoot'] | Should -Be 'D:\RockBackups'
        }

        It 'leaves the backup root alone when the command does not state one' {
            # Optional, so the runbook's C:\RockBackups\production\<utc>-<sha> stays
            # the answer for every deploy CI dispatches. An empty string forwarded
            # here would put the backup at the drive root.
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command = 'deploy-environment'; environmentName = 'production'; sha = 'a'
                    artifactGcsPath = 'g'; hostName = 'h'; backupRoot = '   '
                })

            $plan.Arguments.ContainsKey('BackupRoot') | Should -BeFalse
        }

        It 'will not let a document reach a parameter the row holds out of reach' {
            # Unreachable is a statement about the queue, so it has to be one the
            # binder keeps. A queued document that names the field anyway must still
            # not reach the parameter -- otherwise the list documents something that
            # is not true, which is worse than the silence it replaced.
            #
            # Both spellings a hand-written command would use: the camelCase the rest
            # of the payload is written in, and the parameter's own name, which is
            # what somebody copying from the script would type.
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command = 'deploy-environment'; environmentName = 'production'; sha = 'a'
                    artifactGcsPath = 'g'; hostName = 'h'
                    healthCheckTimeoutSeconds = 3600
                    certificateThumbprint     = 'AA11'
                    SharedAssetSourcePath     = 'C:\inetpub\wwwroot'
                    SharedAssetDirectories    = 'Themes'
                })

            foreach ($held in (Get-CommandContract -Command 'deploy-environment').Unreachable) {
                $plan.Arguments.ContainsKey($held) | Should -BeFalse -Because "$held is declared unreachable"
            }
        }

        It 'is a dry run unless the command asks to apply' {
            $dry = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command = 'deploy-environment'; environmentName = 'production'; sha = 'a'
                    artifactGcsPath = 'g'; hostName = 'h'; apply = $false
                })
            $wet = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command = 'deploy-environment'; environmentName = 'production'; sha = 'a'
                    artifactGcsPath = 'g'; hostName = 'h'; apply = $true
                })

            $dry.Arguments.ContainsKey('Apply') | Should -BeFalse
            $wet.Arguments['Apply'] | Should -BeTrue
        }

        It 'is the only verb handed a deploy timeline to write' {
            # It is the one that takes the site offline, and the only one whose log
            # going quiet costs an operator the window they need.
            $carrying = foreach ($verb in (Get-CommandContract).Keys) {
                $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -StepLogPath $script:StepLog `
                    -Command (script:New-MaximalCommand -Verb $verb)
                if ($plan.Arguments.ContainsKey('StepLogPath')) { $verb }
            }

            @($carrying) | Should -Be @('deploy-environment')
        }
    }

    Context 'renew-certificate' {

        It 'takes nothing from the document and the deploy root from the agent' {
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command = 'renew-certificate'
                })

            @($plan.Arguments.Keys) | Should -Be @('DeployRoot')
            $plan.Arguments['DeployRoot'] | Should -Be $script:DeployRoot
        }
    }

    Context 'the database verbs' {

        It 'refuses the finder without a connection string' {
            # The catalog is behind a PSC endpoint with no public IP and Cloud SQL
            # refuses every login but the owning account, so the string the VM already
            # holds is the only way in. Without it there is nothing to fall back to.
            { Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                        command = 'find-legacy-text-columns'
                    }) } | Should -Throw -ExpectedMessage '*find-legacy-text-columns requires connectionString*'
        }

        It 'leaves the finder metadata-only unless the command asks for sizes' {
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command = 'find-legacy-text-columns'; connectionString = 'Server=x'
                })

            $plan.Arguments.ContainsKey('MeasureSizes') | Should -BeFalse
        }

        It 'refuses the anonymizer without a catalog to rewrite' {
            # Every other optional field on every other command degrades to something
            # sensible when it is missing. This one must not: the value it carries is
            # the operator stating which catalog they mean to destroy contact data in,
            # and absent is not a catalog name.
            { Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                        command = 'anonymize-staging'; connectionString = 'Server=x'
                    }) } | Should -Throw -ExpectedMessage '*anonymize-staging requires expectedCatalog*'
        }

        It 'refuses a catalog stated as whitespace, which is how a dispatch box arrives empty' {
            { Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                        command = 'anonymize-staging'; connectionString = 'Server=x'; expectedCatalog = '   '
                    }) } | Should -Throw -ExpectedMessage '*anonymize-staging requires expectedCatalog*'
        }

        It 'is a dry run unless the command asks to apply' {
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command = 'anonymize-staging'; connectionString = 'Server=x'
                    expectedCatalog = 'RockStaging'; apply = $false
                })

            $plan.Arguments.ContainsKey('Apply') | Should -BeFalse
        }

        It 'forwards the keep list as the values, not as the string it arrived in' {
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command = 'anonymize-staging'; connectionString = 'Server=x'
                    expectedCatalog = 'RockStaging'; keepEmailDomains = ' 268generation.com , passioncitychurch.com '
                })

            @($plan.Arguments['KeepEmailDomains']) | Should -Be @('268generation.com', 'passioncitychurch.com')
        }

        It 'anonymizes everyone when the keep list is absent, as a command written before it existed would' {
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command = 'anonymize-staging'; connectionString = 'Server=x'; expectedCatalog = 'RockStaging'
                })

            $plan.Arguments.ContainsKey('KeepEmailDomains') | Should -BeFalse
        }

        It 'refuses the theme command without the theme it is aimed at' {
            # Defaulting it would pick a theme on the operator's behalf, against a
            # catalog they named explicitly.
            { Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                        command = 'set-theme-customization'; connectionString = 'Server=x'
                    }) } | Should -Throw -ExpectedMessage '*set-theme-customization requires themeName*'
        }

        It 'forwards an empty override block, because empty is how an operator clears it' {
            # The one field here that is decided on presence rather than on blankness.
            # Reading an empty string as absent would make the block impossible to clear.
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command = 'set-theme-customization'; themeName = 'RockNextGen'
                    connectionString = 'Server=x'; customOverrides = ''
                })

            $plan.Arguments.ContainsKey('CustomOverrides') | Should -BeTrue
            $plan.Arguments['CustomOverrides'] | Should -Be ''
        }

        It 'leaves the override block alone when the command does not mention it' {
            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                    command = 'set-theme-customization'; themeName = 'RockNextGen'; connectionString = 'Server=x'
                })

            $plan.Arguments.ContainsKey('CustomOverrides') | Should -BeFalse
        }
    }

    Context 'documents the binder will not act on' {

        It 'refuses a verb with no contract' {
            { Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                        command = 'teleport'
                    }) } | Should -Throw -ExpectedMessage '*Unknown command: teleport*'
        }

        It 'refuses a document that names no command at all' {
            { Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                        prNumber = 7
                    }) } | Should -Throw -ExpectedMessage '*names no command*'
        }
    }
}

Describe 'The arguments the job is handed' {

    It 'binds at the real signature of the script each contract names' {
        # The check the old shape could not express. Every field of every contract,
        # through the serializer Start-Job puts an argument list through, splatted at
        # a parameter list read out of the target script -- so renaming a parameter
        # in a deployment script fails here rather than on the box, minutes into a
        # cutover, as "A parameter cannot be found that matches parameter name".
        $failures = foreach ($verb in (Get-CommandContract).Keys) {
            $contract = Get-CommandContract -Command $verb
            $path = script:Find-ContractScript -Name ([string]$contract.Script)
            if (-not $path) { "$verb names a script that is not published"; continue }

            $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -StepLogPath $script:StepLog `
                -Command (script:New-MaximalCommand -Verb $verb)

            $crossed = [System.Management.Automation.PSSerializer]::Deserialize(
                [System.Management.Automation.PSSerializer]::Serialize($plan.Arguments))

            try {
                $bound = & (script:New-SignatureProbe -Path $path) @crossed
                $unbound = @($crossed.Keys | Where-Object { -not $bound.ContainsKey($_) })
                if ($unbound.Count -gt 0) { "$verb did not bind: $($unbound -join ', ')" }
            }
            catch {
                "$verb could not be bound at $($contract.Script): $($_.Exception.Message)"
            }
        }

        @($failures) -join '; ' | Should -BeNullOrEmpty
    }

    It 'would notice a parameter that had been renamed out from under a contract' {
        # Without this, the sweep above passes just as green against a contract that
        # binds nothing, or against a probe that accepts anything.
        $contract = Get-CommandContract -Command 'anonymize-staging'
        $path = script:Find-ContractScript -Name ([string]$contract.Script)
        $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot `
            -Command (script:New-MaximalCommand -Verb 'anonymize-staging')
        $arguments = $plan.Arguments
        $probe = script:New-SignatureProbe -Path $path -Rename 'ExpectedCatalog'

        { & $probe @arguments } | Should -Throw -ExpectedMessage '*ExpectedCatalog*'
    }

    It 'survives the serializer with its switches and its lists intact' {
        # Start-Job serializes its argument list. A switch arriving as a string or a
        # list arriving as one joined value would both bind without complaint and do
        # the wrong thing quietly.
        $plan = Resolve-CommandArguments -DeployRoot $script:DeployRoot -Command (script:New-Command @{
                command = 'anonymize-staging'; connectionString = 'Server=x'
                expectedCatalog = 'RockStaging'; apply = $true; keepEmailDomains = 'a.com,b.org'
            })

        $crossed = [System.Management.Automation.PSSerializer]::Deserialize(
            [System.Management.Automation.PSSerializer]::Serialize($plan.Arguments))

        $crossed['Apply'] | Should -BeOfType [bool]
        $crossed['Apply'] | Should -BeTrue
        @($crossed['KeepEmailDomains']).Count | Should -Be 2
        @($crossed['KeepEmailDomains'])[1] | Should -Be 'b.org'
    }
}
