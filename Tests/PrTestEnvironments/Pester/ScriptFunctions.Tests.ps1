<#
    The lifter, held to the guarantee it makes.

    Every other suite in this directory starts by lifting functions out of a
    deployment script, so what this file checks sits underneath all of them: that
    a lift which would not stand up on its own is refused at the import rather
    than at whichever It first reaches the missing branch.

    The fixtures are written here rather than taken from Deployment/. What the
    real scripts define is the subject of the other twenty suites; what this one
    is about is the rule, and a rule is easier to read against six lines than
    against three thousand. The deploy script does get one look in -- it sets
    $ErrorActionPreference at the top, which is why the automatic variables have
    to be excluded and why there is a test saying so.
#>

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'ScriptFunctions.psm1') -Force

    # On disk rather than in a string: both public functions take a path, because
    # every real caller has one.
    #
    # The file name is assembled rather than written out. test_powershell_job.py
    # reads every quoted .ps1 literal under this directory as a deploy script that
    # has to exist under Deployment/, and a fixture this suite writes for itself
    # is not one of those.
    $script:FixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) `
        ('scriptfunctions-' + [guid]::NewGuid().ToString('n'))
    New-Item -ItemType Directory -Path $script:FixtureRoot -Force | Out-Null

    # Defined here, and Script:-prefixed, because Pester runs an It in a scope that
    # does not see functions declared in the Describe body.
    function Script:New-Fixture {
        param([string]$Name, [string]$Body)

        $path = Join-Path $script:FixtureRoot ($Name + '.ps1')
        Set-Content -Path $path -Value $Body -Encoding UTF8
        return $path
    }

    # A small call graph: two leaves, one function calling both, one that needs
    # nothing, and one that carries its own nested definition.
    $script:CallGraph = Script:New-Fixture -Name 'call-graph' -Body @'
param(
    [string]$EnvironmentName
)

$ErrorActionPreference = "Stop"
$script:StartedUtc = (Get-Date).ToUniversalTime()

function Write-Line {
    param([string]$Message)
    return "[$script:StartedUtc] $Message"
}

function Get-Greeting {
    param([string]$Name)
    return "hello $Name from $EnvironmentName"
}

function Invoke-Both {
    return (Write-Line -Message (Get-Greeting -Name 'world'))
}

function Get-Sum {
    param([int]$Count)
    $total = 0
    foreach ($index in 1..$Count) { $total += $index }
    return $total
}

Write-Line -Message 'deployed'
'@

    # A nested definition that shadows one of the file's own functions. Nothing
    # else makes the difference visible: a nested name with no twin at the top
    # level is not offered by the file, so it is never a name the import could
    # have demanded in the first place.
    $script:Shadowing = Script:New-Fixture -Name 'shadowing' -Body @'
function Write-Line {
    param([string]$Message)
    return "script: $Message"
}

function Invoke-WithNested {
    function Write-Line {
        param([string]$Message)
        return "nested: $Message"
    }

    return (Write-Line -Message 'inner')
}
'@

    # One function per way a name can end up on the left of an `=` or inside a
    # nested scope. Both script-scope names are ones a body could plausibly read,
    # so anything the binder gets wrong shows up as a demand that is or is not made.
    $script:Binder = Script:New-Fixture -Name 'binder' -Body @'
$ErrorActionPreference = "Stop"
$script:Ambient = [pscustomobject]@{ Rows = @() }
$script:Rows = @(0, 0, 0)

function Set-Member {
    $Ambient.Rows = @('one')
}

function Set-Element {
    $Rows[0] = 1
}

function Use-Cast {
    [int]$Ambient = 0
    $Ambient++
    return $Ambient
}

function Use-MultipleAssignment {
    $Ambient, $Rows = 1, 2
    return @($Ambient, $Rows)
}

function Use-Preference {
    if ($ErrorActionPreference -ne 'Stop') { throw 'expected Stop' }
    return $PSScriptRoot
}

function Use-ScriptBlockParameter {
    $work = { param($Ambient) $Ambient * 2 }
    return (& $work 21)
}
'@

    $script:Unparsable = Script:New-Fixture -Name 'unparsable' -Body @'
function Get-Half {
    return 'half a brace'
'@
}

AfterAll {
    if ($script:FixtureRoot -and (Test-Path $script:FixtureRoot)) {
        Remove-Item $script:FixtureRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Describe 'Import-ScriptFunction' {

    It 'lifts a function that needs nothing, and asks for nothing' {
        . (Import-ScriptFunction -Path $script:CallGraph -Name 'Get-Sum')

        Get-Sum -Count 4 | Should -Be 10
    }

    It 'refuses a lift whose callees were left behind' {
        { Import-ScriptFunction -Path $script:CallGraph -Name 'Invoke-Both' } |
            Should -Throw -ExpectedMessage '*Write-Line*'
    }

    It 'names every callee, not just the first one it hit' {
        $message = ''
        try { Import-ScriptFunction -Path $script:CallGraph -Name 'Invoke-Both' }
        catch { $message = $_.Exception.Message }

        $message | Should -Match 'Get-Greeting'
        $message | Should -Match 'Write-Line'
    }

    It 'says which lifted function reached for it, so the fix is obvious' {
        # The list on its own is a list of names. What makes it actionable is
        # knowing the body they came out of -- with four functions lifted and one
        # name missing, that is the difference between reading the message and
        # reading the deploy script.
        $message = ''
        try { Import-ScriptFunction -Path $script:CallGraph -Name 'Invoke-Both' }
        catch { $message = $_.Exception.Message }

        $message | Should -Match 'Write-Line -- a function Invoke-Both calls'
    }

    It 'refuses a lift that reads a value only the script sets' {
        # $EnvironmentName is a parameter of the script, not of the function. A
        # suite lifting this one alone gets an empty greeting and no warning.
        { Import-ScriptFunction -Path $script:CallGraph -Name 'Get-Greeting' } |
            Should -Throw -ExpectedMessage '*EnvironmentName*'
    }

    It 'counts a script-scope assignment as something the script supplies' {
        { Import-ScriptFunction -Path $script:CallGraph -Name 'Write-Line' } |
            Should -Throw -ExpectedMessage '*StartedUtc*'
    }

    It 'accepts the same lift once the caller declares what it provides' {
        . (Import-ScriptFunction -Path $script:CallGraph -Name 'Get-Greeting' `
                -Supplied 'EnvironmentName')

        $script:EnvironmentName = 'staging'
        Get-Greeting -Name 'world' | Should -Be 'hello world from staging'
    }

    It 'lifts a whole call graph, and then it runs' {
        . (Import-ScriptFunction -Path $script:CallGraph `
                -Name 'Invoke-Both', 'Write-Line', 'Get-Greeting' `
                -Supplied 'StartedUtc', 'EnvironmentName')

        $script:StartedUtc = [datetime]'2026-09-15T00:00:00Z'
        $script:EnvironmentName = 'production'

        Invoke-Both | Should -Match 'hello world from production'
    }

    It 'ships a nested function inside its parent, and asks for nothing' {
        # Invoke-WithNested defines its own Write-Line, shadowing the one at the
        # top of the same file. The nested copy is inside the parent's extent, so
        # it arrives with it -- and demanding the caller declare `Write-Line`
        # would be demanding a stand-in for a function that is right there in the
        # body being lifted.
        . (Import-ScriptFunction -Path $script:Shadowing -Name 'Invoke-WithNested')

        Invoke-WithNested | Should -Be 'nested: inner'
    }

    It 'refuses a name the file defines twice, rather than picking one' {
        # Which Write-Line ships is not a question the import should be answering
        # quietly. It is the same refusal as an unknown name, for the same reason:
        # the suite would go on asserting, against whichever one it happened to get.
        { Import-ScriptFunction -Path $script:Shadowing -Name 'Write-Line' } |
            Should -Throw -ExpectedMessage '*more than once*'
    }

    It 'refuses a declaration that nothing reaches for' {
        # The other direction, and the one the caller cannot notice on its own. A
        # stand-in the real function has stopped calling goes on being defined,
        # and the suite goes on passing over a branch that no longer exists.
        { Import-ScriptFunction -Path $script:CallGraph -Name 'Get-Sum' -Supplied 'Write-Line' } |
            Should -Throw -ExpectedMessage '*Write-Line*'
    }

    It 'refuses a name the file does not define' {
        { Import-ScriptFunction -Path $script:CallGraph -Name 'Get-Nothing' } |
            Should -Throw -ExpectedMessage '*defines no function*'
    }

    It 'lists what the file does define, because the name is usually a rename' {
        $message = ''
        try { Import-ScriptFunction -Path $script:CallGraph -Name 'Get-Nothing' }
        catch { $message = $_.Exception.Message }

        $message | Should -Match 'Get-Greeting'
        $message | Should -Match 'Get-Sum'
    }
}

Describe 'What a lifted body binds for itself' {

    It 'treats a property assignment as a read of the variable, not a binding' {
        # `$Ambient.Rows = @()` cannot run unless $Ambient already holds an object,
        # so the body depends on the script scope every bit as much as a plain read
        # does. Reading through the member to call it a binding was the first shape
        # of this, and it was wrong in the direction that passes.
        { Import-ScriptFunction -Path $script:Binder -Name 'Set-Member' } |
            Should -Throw -ExpectedMessage '*Ambient*'
    }

    It 'treats an element assignment the same way' {
        { Import-ScriptFunction -Path $script:Binder -Name 'Set-Element' } |
            Should -Throw -ExpectedMessage '*Rows*'
    }

    It 'sees a binding behind a cast' {
        { Import-ScriptFunction -Path $script:Binder -Name 'Use-Cast' } |
            Should -Not -Throw
    }

    It 'sees every name on the left of a multiple assignment' {
        { Import-ScriptFunction -Path $script:Binder -Name 'Use-MultipleAssignment' } |
            Should -Not -Throw
    }

    It 'never asks a caller to supply a variable PowerShell supplies' {
        # Not a hypothetical exclusion. Both deployment scripts open with
        # `$ErrorActionPreference = "Stop"` at the top level, which makes it
        # something the script sets -- so without this, every lift in this
        # directory would be told to declare it.
        { Import-ScriptFunction -Path $script:Binder -Name 'Use-Preference' } |
            Should -Not -Throw
    }

    It 'sees a scriptblock parameter as bound inside the body that declares it' {
        # `{ param($node) ... }` handed to FindAll is the common shape. The name is
        # the block's own, and demanding it would be demanding a stand-in for
        # something that never leaves the function.
        { Import-ScriptFunction -Path $script:Binder -Name 'Use-ScriptBlockParameter' } |
            Should -Not -Throw
    }
}

Describe 'Get-ScriptFunctionText' {

    It 'hands back the source text, character for character' {
        $text = Get-ScriptFunctionText -Path $script:CallGraph -Name 'Get-Greeting'

        $text | Should -Match 'function Get-Greeting'
        $text | Should -Match 'hello \$Name from \$EnvironmentName'
    }

    It 'reads one function out of the middle of a call graph, undeclared' {
        # The whole difference between the two. Import-ScriptFunction refuses this
        # exact call because dot-sourcing the result would leave Write-Line and
        # Get-Greeting dangling; a test that only reads the characters is not going
        # to dot-source anything, and has nothing to promise.
        { Get-ScriptFunctionText -Path $script:CallGraph -Name 'Invoke-Both' } |
            Should -Not -Throw
    }

    It 'joins several in the order they were asked for' {
        $text = Get-ScriptFunctionText -Path $script:CallGraph -Name 'Get-Sum', 'Get-Greeting'

        $text.IndexOf('function Get-Sum') | Should -BeLessThan $text.IndexOf('function Get-Greeting')
    }

    It 'refuses a name the file does not define, the same as the other one' {
        { Get-ScriptFunctionText -Path $script:CallGraph -Name 'Get-Nothing' } |
            Should -Throw -ExpectedMessage '*defines no function*'
    }
}

Describe 'Against the scripts that ship' {

    BeforeAll {
        $script:DeploymentDir = Get-RepositoryPath 'Deployment/PrTestEnvironments'

        # A second reading of the same files, on purpose not the one under test.
        # The extractor finds functions with the parser; this finds them with a
        # regex anchored at column zero. An AST walk that dropped a function would
        # agree with itself and disagree with this.
        function Script:Get-DeclaredName {
            param([string]$Path)

            return @([regex]::Matches((Get-Content -Path $Path -Raw),
                    '(?m)^function\s+([A-Za-z][\w-]*)') | ForEach-Object { $_.Groups[1].Value })
        }
    }

    It 'hands back the file own characters, for every function in every script' {
        # The fixtures above are six lines each, and any extraction gets those
        # right. These are not: Save-UnhealthyDiagnostics alone carries 37 braces
        # inside quoted strings, and Write-RuntimeConfiguration carries a
        # here-string. Brace counting produces something that parses and is not
        # the function. Asserting the result is a verbatim substring of the file
        # is the extent promise said outright.
        $scripts = @(Get-ChildItem -Path $script:DeploymentDir -Filter *.ps1 | Sort-Object Name)
        $checked = @()

        foreach ($script in $scripts) {
            $raw = Get-Content -Path $script.FullName -Raw
            foreach ($name in (Script:Get-DeclaredName -Path $script.FullName)) {
                $text = Get-ScriptFunctionText -Path $script.FullName -Name $name
                $raw.Contains($text) |
                    Should -BeTrue -Because "$($script.Name) / $name should come back as the file's own text"
                $checked += "$($script.Name)/$name"
            }
        }

        # Not a count, a floor: the sweep has to have seen more than one script and
        # more than one function per script on average, or it found nothing and
        # said so in green.
        $scripts.Count | Should -BeGreaterThan 1
        $checked.Count | Should -BeGreaterThan $scripts.Count
    }

    It 'hands back something that stands up as a script on its own' {
        # What the extent is for. A lifted body that does not parse is one Pester
        # reports as a failure inside the suite that lifted it, wherever that is.
        $scripts = @(Get-ChildItem -Path $script:DeploymentDir -Filter *.ps1 | Sort-Object Name)

        foreach ($script in $scripts) {
            $names = @(Script:Get-DeclaredName -Path $script.FullName)
            if ($names.Count -eq 0) { continue }

            $text = Get-ScriptFunctionText -Path $script.FullName -Name $names

            $errors = $null
            $tokens = $null
            [void][System.Management.Automation.Language.Parser]::ParseInput(
                $text, [ref]$tokens, [ref]$errors)

            @($errors).Count |
                Should -Be 0 -Because "every function in $($script.Name), lifted together, should parse"
        }
    }
}

Describe 'A script that does not parse' {

    It 'says so, and says which file' {
        # Rather than handing back an empty definition list, which reads as a file
        # that defines nothing and fails four lines later as a missing name.
        { Get-ScriptFunctionText -Path $script:Unparsable -Name 'Get-Half' } |
            Should -Throw -ExpectedMessage '*does not parse*'
    }
}
