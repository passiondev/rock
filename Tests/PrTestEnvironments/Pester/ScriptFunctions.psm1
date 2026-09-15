<#
.SYNOPSIS
    Load functions out of a deployment script without running the script.

.DESCRIPTION
    The deployment scripts are scripts, not modules: dot-sourcing one to reach a
    function would also run its body, which deploys a website. A module would
    solve that and cannot be used here -- the bootstrap ships this directory with
    `gsutil cp Deployment/PrTestEnvironments/*.ps1`, so a .psm1 would never reach
    the VM. That constraint is recorded at the top of Invoke-PrEnvironmentCleanup.ps1
    and it is why these tests read the functions out instead.

    The extent comes from the parser rather than from brace counting, so what runs
    under Pester is the exact source text that ships, character for character.

    A lifted function is not the whole of what it needs. It can call another
    function from the same file, and it can read a variable the script sets at the
    top -- `Write-DeployStep` does both. `Import-ScriptFunction` therefore states
    the whole edge of what it lifts: anything a lifted body reaches for is lifted
    too, or named in -Supplied to say the suite provides it. Neither, and the
    import throws, naming what is missing and where it came from.

    So the declaration at the top of a suite is that suite's call graph, and it
    goes stale loudly. Before it existed, the answer lived in whichever suites had
    already met the function: `Save-UnhealthyDiagnostics` was lifted for months
    into a scope holding five of the seven deploy parameters it reads, and the
    line it writes about the deploy's mode was asserted against a blank.

    `Get-ScriptFunctionText` is the other half, for the assertions that read the
    source rather than run it. It lifts the same way and promises nothing about
    what would happen if you ran the result, because its callers are not going to.

    This file is under Tests/ and is never copied to a VM.
#>

Set-StrictMode -Version Latest

# The variables PowerShell supplies on its own. A lifted body reading one of
# these is still self-contained, so none of them is ever something a caller has
# to hand in.
$script:AutomaticVariableNames = @(
    '_', 'PSItem', 'args', 'input', 'this', 'true', 'false', 'null',
    'PSScriptRoot', 'PSCommandPath', 'MyInvocation', 'PSBoundParameters', 'PSCmdlet',
    'Error', 'Host', 'PWD', 'HOME', 'PID', 'LASTEXITCODE', 'Matches', 'foreach', 'switch',
    'ErrorActionPreference', 'ProgressPreference', 'WarningPreference', 'VerbosePreference',
    'InformationPreference', 'ConfirmPreference', 'WhatIfPreference', 'DebugPreference',
    'IsWindows', 'IsLinux', 'IsMacOS', 'StackTrace', 'ExecutionContext', 'OutputEncoding'
)

function Get-BareVariableName {
    # `$script:Foo`, `$using:Foo` and `$Foo` are one name as far as scope goes
    # here: whatever the lifted body ends up reading out of the caller's session.
    param([Parameter(Mandatory = $true)]$Variable)

    return ($Variable.VariablePath.UserPath -replace '^(script|global|local|private|using):', '')
}

function Get-AssignedVariableName {
    # The names an assignment binds -- which is fewer than the names it mentions.
    # `[int]$total = 0` and `$first, $second = 1, 2` bind; `$report.Rows = @()` and
    # `$rows[0] = 1` do not, because setting a property or an element requires the
    # variable to already hold something, so both of those are reads of a name that
    # came from somewhere else.
    #
    # Casts, attributes and parens are unwrapped because they sit between `=` and a
    # variable that really is being bound. Members and indexes are not, and reading
    # through them was the first shape of this function: it bound `report`, which
    # would let a lifted body read a script-scope object and be called
    # self-contained. Wrong in the direction that passes.
    param([Parameter(Mandatory = $true)]$Target)

    $node = $Target
    while ($true) {
        if ($node -is [System.Management.Automation.Language.AttributedExpressionAst]) { $node = $node.Child; continue }
        if ($node -is [System.Management.Automation.Language.ParenExpressionAst]) { $node = $node.Pipeline; continue }
        break
    }

    if ($node -is [System.Management.Automation.Language.ArrayLiteralAst]) {
        return @($node.Elements | Where-Object { $_ -is [System.Management.Automation.Language.VariableExpressionAst] } | ForEach-Object { Get-BareVariableName -Variable $_ })
    }
    if ($node -is [System.Management.Automation.Language.VariableExpressionAst]) {
        return @(Get-BareVariableName -Variable $node)
    }
    return @()
}

function Get-FreeVariableName {
    <#
    .SYNOPSIS
        The variables one function definition reads without binding them first.

    .DESCRIPTION
        Everything the body binds for itself is subtracted: its own parameters,
        the parameters of every scriptblock inside it -- `param($node)` handed to
        FindAll, `param($Bucket)` handed to Start-Job -- every plain assignment,
        and every foreach variable. What is left is what the body expects to find
        in whatever scope it is called from.

        The caller decides which of those matter. Most are nothing: a name no
        deployment script defines is a name this cannot say anything useful
        about.

        Scope is flattened rather than tracked: a name bound anywhere in the body
        counts as bound throughout it, so a scriptblock parameter sharing a name
        with something read outside that block would hide the read. It is the one
        place this errs quietly, and it stays -- the names in question are `node`
        and `candidate`, and tracking scope properly costs more than that is worth.
    #>
    [CmdletBinding()]
    [OutputType([string[]])]
    param([Parameter(Mandatory = $true)]$Definition)

    $bound = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)

    if ($Definition.Parameters) {
        foreach ($parameter in $Definition.Parameters) {
            [void]$bound.Add((Get-BareVariableName -Variable $parameter.Name))
        }
    }
    foreach ($block in $Definition.Body.FindAll({ param($node) $node -is [System.Management.Automation.Language.ScriptBlockAst] }, $true)) {
        if ($block.ParamBlock) {
            foreach ($parameter in $block.ParamBlock.Parameters) {
                [void]$bound.Add((Get-BareVariableName -Variable $parameter.Name))
            }
        }
    }
    foreach ($nested in $Definition.Body.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true)) {
        if ($nested.Parameters) {
            foreach ($parameter in $nested.Parameters) {
                [void]$bound.Add((Get-BareVariableName -Variable $parameter.Name))
            }
        }
    }
    foreach ($assignment in $Definition.Body.FindAll({ param($node) $node -is [System.Management.Automation.Language.AssignmentStatementAst] }, $true)) {
        foreach ($name in (Get-AssignedVariableName -Target $assignment.Left)) {
            [void]$bound.Add($name)
        }
    }
    foreach ($loop in $Definition.Body.FindAll({ param($node) $node -is [System.Management.Automation.Language.ForEachStatementAst] }, $true)) {
        [void]$bound.Add((Get-BareVariableName -Variable $loop.Variable))
    }

    $free = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($variable in $Definition.Body.FindAll({ param($node) $node -is [System.Management.Automation.Language.VariableExpressionAst] }, $true)) {
        $bare = Get-BareVariableName -Variable $variable
        if ($script:AutomaticVariableNames -contains $bare) { continue }
        if ($bound.Contains($bare)) { continue }
        [void]$free.Add($bare)
    }

    return @($free)
}

function Get-ScriptFunctionSurface {
    <#
    .SYNOPSIS
        Read one script file and say what it defines and what it supplies.

    .DESCRIPTION
        `Definitions` is every function in the file, nested ones included, because
        that is what a name can be resolved against. `Offered` is the subset a
        lift can actually satisfy -- a function defined inside another function
        arrives with its parent or not at all. `Supplied` is every variable the
        script itself sets: its parameters, and every assignment outside a
        function body. Those are the names a lifted body can read and find,
        on the VM, without anyone thinking about it.
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory = $true)][string]$Path)

    $resolved = (Resolve-Path -Path $Path).Path

    $errors = $null
    $tokens = $null
    $ast = [System.Management.Automation.Language.Parser]::ParseFile($resolved, [ref]$tokens, [ref]$errors)
    # Wrapped because Set-StrictMode makes `.Count` on a scalar or `$null` an error,
    # and both are shapes the parser hands back.
    if (@($errors).Count -gt 0) {
        throw "$resolved does not parse: $($errors[0].Message)"
    }

    $definitions = @($ast.FindAll(
            { param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] },
            $true))

    $isNested = {
        param($candidate)
        foreach ($other in $definitions) {
            if ($other -eq $candidate) { continue }
            if ($other.Extent.StartOffset -le $candidate.Extent.StartOffset -and
                $other.Extent.EndOffset -ge $candidate.Extent.EndOffset) {
                return $true
            }
        }
        return $false
    }

    $offered = @($definitions | Where-Object { -not (& $isNested $_) } | ForEach-Object { $_.Name })

    $supplied = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    if ($ast.ParamBlock) {
        foreach ($parameter in $ast.ParamBlock.Parameters) {
            [void]$supplied.Add((Get-BareVariableName -Variable $parameter.Name))
        }
    }
    foreach ($assignment in $ast.FindAll({ param($node) $node -is [System.Management.Automation.Language.AssignmentStatementAst] }, $true)) {
        if (& $isNested $assignment) { continue }
        foreach ($name in (Get-AssignedVariableName -Target $assignment.Left)) {
            [void]$supplied.Add($name)
        }
    }

    return [pscustomobject]@{
        Path        = $resolved
        Definitions = $definitions
        Offered     = $offered
        Supplied    = @($supplied)
    }
}

function Select-ScriptFunctionDefinition {
    <#
    .SYNOPSIS
        The definitions for the requested names, in the order they were asked for.

    .DESCRIPTION
        A name that is not in the file is a terminating error rather than a
        silent omission. Skipping it would leave the suite asserting against
        functions that no longer exist, and it would report the same green it
        always did.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]$Surface,
        [Parameter(Mandatory = $true)][string[]]$Name
    )

    return @(foreach ($wanted in $Name) {
            $match = @($Surface.Definitions | Where-Object { $_.Name -eq $wanted })
            if ($match.Count -eq 0) {
                $available = ($Surface.Definitions | ForEach-Object { $_.Name } | Sort-Object) -join ', '
                throw "$($Surface.Path) defines no function '$wanted'. It defines: $available."
            }
            if ($match.Count -gt 1) {
                throw "$($Surface.Path) defines '$wanted' more than once, so it is ambiguous which one ships."
            }
            $match[0]
        })
}

function Get-ScriptFunctionText {
    <#
    .SYNOPSIS
        The source text of the named functions, for a test that reads rather than runs.

    .DESCRIPTION
        Five assertions in DeployVerification.Tests.ps1 want the characters --
        that the version check reads `VersionInfo.FileVersion` and not a hash,
        that the smoke check probes `https://127.0.0.1/Login`. They were spelled
        `(Import-ScriptFunction ...).ToString()`, which asks for something
        callable and then throws the callable part away.

        Keeping them on that spelling would have cost more than the noise. What
        `Import-ScriptFunction` now promises is that dot-sourcing its result
        leaves nothing dangling, and a caller reading one function out of the
        middle of a call graph cannot promise that and does not need to. Two
        intents, two functions, and the one carrying the guarantee is the one
        every suite dot-sources.

    .PARAMETER Path
        The script to read. Not executed.

    .PARAMETER Name
        The functions to read, in any order.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string[]]$Name
    )

    $surface = Get-ScriptFunctionSurface -Path $Path
    $wanted = Select-ScriptFunctionDefinition -Surface $surface -Name $Name

    return (@($wanted | ForEach-Object { $_.Extent.Text }) -join "`n`n")
}

function Import-ScriptFunction {
    <#
    .SYNOPSIS
        Return a scriptblock defining the named functions from a script file.

    .DESCRIPTION
        Dot-source the result to bring the functions into the caller's scope:

            . (Import-ScriptFunction -Path $script -Name 'Get-RedactedText')

        What comes back is self-contained, or this throws. A lifted function can
        call another function from the same file, and it can read a variable the
        script sets at the top -- `Write-DeployStep` does both. Lift one and not
        the other and nothing says so here; the suite fails later, at the first
        It that happens to reach that branch, with `The term 'Write-DeployStep'
        is not recognized` and no mention of the script it came out of. Suites
        that had already met it carried the answer as a habit rather than a rule,
        which is why `Save-UnhealthyDiagnostics` was lifted for months into a
        scope holding five of the seven deploy parameters it reads.

        So the import states the whole edge of what it lifts. Anything a lifted
        body reaches for is lifted too, or named in -Supplied to say the caller
        provides it. That list is the call graph, written at the top of the suite
        where somebody reading the suite will see it.

    .PARAMETER Path
        The script to read. Not executed.

    .PARAMETER Name
        The functions to lift out, in any order.

    .PARAMETER Supplied
        Names the lifted bodies reach for that the caller provides itself: a
        stand-in function defined in the suite, a Mock, or a script-scope
        variable set in BeforeAll. A name here that nothing reaches for is an
        error too -- a stand-in nobody needs is one the real function has since
        outgrown, and it would go on passing.
    #>
    [CmdletBinding()]
    [OutputType([scriptblock])]
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string[]]$Name,
        [string[]]$Supplied = @()
    )

    $surface = Get-ScriptFunctionSurface -Path $Path
    $wanted = Select-ScriptFunctionDefinition -Surface $surface -Name $Name

    # What the lift itself provides: the functions asked for, plus any function
    # defined inside one of them, which ships inside its parent's extent.
    $provided = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($definition in $wanted) {
        [void]$provided.Add($definition.Name)
        foreach ($nested in $definition.Body.FindAll({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] }, $true)) {
            [void]$provided.Add($nested.Name)
        }
    }

    $reached = [ordered]@{}
    foreach ($definition in $wanted) {
        foreach ($call in $definition.Body.FindAll({ param($node) $node -is [System.Management.Automation.Language.CommandAst] }, $true)) {
            $called = $call.GetCommandName()
            if (-not $called) { continue }
            if ($surface.Offered -notcontains $called) { continue }
            if ($provided.Contains($called)) { continue }
            if (-not $reached.Contains($called)) {
                $reached[$called] = "a function $($definition.Name) calls"
            }
        }
        foreach ($free in (Get-FreeVariableName -Definition $definition)) {
            if ($surface.Supplied -notcontains $free) { continue }
            if (-not $reached.Contains($free)) {
                $reached[$free] = "a value the script sets and $($definition.Name) reads"
            }
        }
    }

    $unmet = @($reached.Keys | Where-Object { $Supplied -notcontains $_ })
    if ($unmet.Count -gt 0) {
        $listed = ($unmet | ForEach-Object { "      $_ -- $($reached[$_])" }) -join "`n"
        throw @"
$($surface.Path) cannot be lifted as asked: what comes back would not stand up on its own.

  Reached for, and neither lifted nor declared:
$listed

  Lift each one by adding it to -Name, or name it in -Supplied to say this suite provides it.
"@
    }

    $stale = @($Supplied | Where-Object { -not $reached.Contains($_) })
    if ($stale.Count -gt 0) {
        throw "$($surface.Path): -Supplied names $($stale -join ', '), which nothing lifted here reaches for. A stand-in nobody calls is one the real function has outgrown; drop it, or lift the function that needs it."
    }

    return [scriptblock]::Create((@($wanted | ForEach-Object { $_.Extent.Text }) -join "`n`n"))
}

function Get-RepositoryPath {
    <#
    .SYNOPSIS
        Resolve a path from the repository root.

    .DESCRIPTION
        Every suite in this directory reaches back out of Tests/ to the scripts it
        loads, and each one used to spell that as `../../../`. Six copies of a
        relative hop is six things to fix the day this directory moves, and five of
        them would still resolve to somewhere -- just not to the repository root.

        A path that does not exist is a terminating error. The alternative is a
        string that fails later, inside whatever tried to read it, naming a
        directory nobody recognises.

    .PARAMETER Path
        Where to go, relative to the repository root. Forward slashes are fine on
        Windows; Join-Path normalises them.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)][string]$Path
    )

    # $PSScriptRoot inside a module function is the module's own directory, so the
    # hop is written once, here, and nowhere else.
    $root = (Resolve-Path -Path (Join-Path $PSScriptRoot '../../..')).Path

    $combined = Join-Path $root $Path
    if (-not (Test-Path -Path $combined)) {
        throw "The repository has no '$Path'. Looked under $root."
    }

    return (Resolve-Path -Path $combined).Path
}


# The cmdlets whose real return shape is not single-valued, and the property a
# caller has to be able to read off each shape. Adding an entry here is what
# brings a cmdlet under Get-IisReturnShape and under the guard in
# MockShapeContract.Tests.ps1; nothing else has to change.
#
# Get-ItemProperty is here because it is the one that got through. See
# Resolve-DeploymentTarget in Deploy-RockEnvironment.ps1: on the IIS provider
# version the scheduled task runs, -Name applicationPool returns a bare String,
# and on the one the tests assumed it returns a ConfigurationAttribute with a
# .Value. The mock returned only the second, `.Value` was dereferenced
# unconditionally, and 269 tests stayed green over a line that was a terminating
# error the first time production reached it.
$script:ShapeVaryingCmdlets = @{
    'Get-ItemProperty' = 'ApplicationPool'
}

function Get-IisReturnShape {
    <#
    .SYNOPSIS
        Every shape the real cmdlet can hand back for one logical value.

    .DESCRIPTION
        A mock is a claim about what the real API returns, and a mock that
        returns one shape where the API returns two is a test that proves the
        code works against the half it was written for. That is not a
        hypothetical: it is how a production-breaking dereference shipped under
        a full green suite on 2026-09-14.

        So suites do not hand-write these. They loop:

            foreach ($shape in Get-IisReturnShape -Kind ApplicationPool -Value 'RockProdPool') {
                Mock Get-ItemProperty { $shape }
                ...
            }

        and a new shape discovered on a new host is added once, here, and every
        suite that mocks through this helper starts covering it.

    .PARAMETER Kind
        Which logical value is being faked.

    .PARAMETER Value
        The value itself, as a string. Each returned shape carries it.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][ValidateSet('ApplicationPool')][string]$Kind,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Value
    )

    switch ($Kind) {
        'ApplicationPool' {
            # Order is deliberate: the bare String first, because it is the shape
            # the scheduled task actually gets and the one a suite written on a
            # developer's machine will not think of.
            return @(
                $Value,
                [pscustomobject]@{ Value = $Value }
            )
        }
    }
}

function Get-ShapeVaryingCmdlet {
    <#
    .SYNOPSIS
        The cmdlet names currently under the mock-shape contract.

    .DESCRIPTION
        Read by MockShapeContract.Tests.ps1 so the registry lives in one place
        rather than being restated in the test that enforces it.
    #>
    [CmdletBinding()]
    [OutputType([string[]])]
    param()

    return @($script:ShapeVaryingCmdlets.Keys | Sort-Object)
}

Export-ModuleMember -Function Import-ScriptFunction, Get-ScriptFunctionText, Get-RepositoryPath, Get-IisReturnShape, Get-ShapeVaryingCmdlet
