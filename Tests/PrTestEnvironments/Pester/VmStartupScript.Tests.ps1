BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'ScriptFunctions.psm1') -Force
    $script:ActionScript = Get-RepositoryPath '.github/actions/inject-startup-script/Set-VmStartupScript.ps1'
    . (Import-ScriptFunction -Path $script:ActionScript -Name 'Test-StartupScript', 'Get-StartupScriptRejection')
}

# Fixtures are written without an extension on purpose. The functions parse the
# file's text rather than dispatching on its name, and a bare *.ps1 literal in a
# Pester file is read by test_powershell_job.py as a deployment script this suite
# claims to cover.
Describe 'Test-StartupScript' {
    It 'reports a script that was never written' {
        $report = Test-StartupScript -Path (Join-Path $TestDrive 'nothing-here')

        $report.Exists | Should -BeFalse
        $report.Length | Should -Be 0
    }

    It 'reports a path that is blank' {
        (Test-StartupScript -Path '').Exists | Should -BeFalse
    }

    It 'reports length and no complaints for a script that parses' {
        $path = Join-Path $TestDrive 'good'
        Set-Content -LiteralPath $path -Value 'Write-Host "hello"' -NoNewline

        $report = Test-StartupScript -Path $path

        $report.Exists | Should -BeTrue
        $report.IsBlank | Should -BeFalse
        $report.Length | Should -Be 18
        $report.ParseErrors.Count | Should -Be 0
    }

    It 'reports the parse errors in PowerShell that does not compile' {
        $path = Join-Path $TestDrive 'broken'
        Set-Content -LiteralPath $path -Value 'if ($true) { Write-Host "unclosed"'

        $report = Test-StartupScript -Path $path

        $report.Exists | Should -BeTrue
        $report.ParseErrors.Count | Should -BeGreaterThan 0
    }

    It 'reports whitespace as blank even though it has a length' {
        # The shape the failure arrives in: a here-string whose interpolations all
        # resolved to nothing still carries its newlines and its indentation.
        $path = Join-Path $TestDrive 'whitespace'
        Set-Content -LiteralPath $path -Value "   `n   `n"

        $report = Test-StartupScript -Path $path

        $report.IsBlank | Should -BeTrue
        $report.Length | Should -BeGreaterThan 0
    }
}

Describe 'Get-StartupScriptRejection' {
    It 'passes a script that exists, has content and parses' {
        $report = [ordered]@{ Exists = $true; IsBlank = $false; Length = 42; ParseErrors = @() }

        Get-StartupScriptRejection -Report $report -Path 'x' | Should -BeNullOrEmpty
    }

    It 'refuses to stage when nothing was written, and names the path' {
        $report = [ordered]@{ Exists = $false; IsBlank = $true; Length = 0; ParseErrors = @() }

        $rejection = Get-StartupScriptRejection -Report $report -Path '/tmp/startup'

        $rejection | Should -Match '/tmp/startup'
        $rejection | Should -Match 'Staging nothing'
    }

    It 'refuses an empty script and says it would look like a success' {
        # The whole reason this check exists: staging is silent, the boot is minutes
        # later, and the machine comes up having run nothing.
        $report = [ordered]@{ Exists = $true; IsBlank = $true; Length = 0; ParseErrors = @() }

        Get-StartupScriptRejection -Report $report -Path 'x' | Should -Match 'installing nothing'
    }

    It 'catches a blank script before the parse check can pass it' {
        # Ordering, asserted rather than assumed: whitespace parses cleanly, so a
        # parse-first rejection would let the empty payload through.
        $report = [ordered]@{ Exists = $true; IsBlank = $true; Length = 9; ParseErrors = @() }

        Get-StartupScriptRejection -Report $report -Path 'x' | Should -Match 'empty'
    }

    It 'refuses PowerShell that does not parse, and quotes the first error' {
        $report = [ordered]@{
            Exists      = $true
            IsBlank     = $false
            Length      = 120
            ParseErrors = @([pscustomobject]@{ Message = 'Missing closing brace.' })
        }

        $rejection = Get-StartupScriptRejection -Report $report -Path 'x'

        $rejection | Should -Match 'Missing closing brace'
        $rejection | Should -Match 'does not parse'
    }

    It 'says what the reader has to know: nothing checks this between here and the boot' {
        $report = [ordered]@{
            Exists      = $true
            IsBlank     = $false
            Length      = 120
            ParseErrors = @([pscustomobject]@{ Message = 'Missing closing brace.' })
        }

        Get-StartupScriptRejection -Report $report -Path 'x' | Should -Match 'stage, reboot, and run nothing'
    }
}
