<#
    A mock is a claim about what the real API returns. When the claim is narrower
    than the API, the suite proves the code works against the half it was written
    for and says nothing about the other half.

    That is not a hypothetical here. On 2026-09-14 the production dry run
    (34911854406) failed on this line in Resolve-DeploymentTarget:

        $poolName = (Get-ItemProperty "IIS:\Sites\$TargetSiteName" -Name applicationPool).Value

    Windows PowerShell 5.1's WebAdministration provider returns a bare String
    from that call. `.Value` on a String is $null under normal rules and a
    terminating error under Set-StrictMode, which the script sets. The Pester
    mock returned the ConfigurationAttribute shape -- the one with a .Value --
    so all 269 tests were green over a line that could not run. InPlace is the
    production-only branch, so no staging deploy had ever reached it either.

    The fix in the script reads both shapes. This file is the fix in the tests:
    the shapes live in one registry in ScriptFunctions.psm1, and a suite that
    mocks a registered cmdlet has to take its return values from there, so a
    shape added to the registry is covered everywhere at once rather than
    wherever somebody remembers.
#>

Import-Module (Join-Path $PSScriptRoot 'ScriptFunctions.psm1') -Force

# Assembled rather than written out. test_powershell_job.py reads every quoted
# .ps1 literal under this directory as a script that must exist under
# Deployment/, and a glob over the suites is not one.
$script:SuiteGlob = '*.Tests' + '.ps1'

Describe 'Get-IisReturnShape' {

    It 'returns more than one shape, which is the entire reason it exists' {
        @(Get-IisReturnShape -Kind ApplicationPool -Value 'RockProdPool').Count |
            Should -BeGreaterThan 1
    }

    It 'includes the bare string, which is what production returned' {
        $shapes = @(Get-IisReturnShape -Kind ApplicationPool -Value 'RockProdPool')

        ($shapes | Where-Object { $_ -is [string] }) | Should -Not -BeNullOrEmpty
    }

    It 'includes the object shape carrying the value on .Value' {
        $shapes = @(Get-IisReturnShape -Kind ApplicationPool -Value 'RockProdPool')
        $withValue = @($shapes | Where-Object { $_ -isnot [string] -and $null -ne $_.PSObject.Properties['Value'] })

        $withValue.Count | Should -Be 1
        $withValue[0].Value | Should -Be 'RockProdPool'
    }

    It 'carries the caller''s value on every shape it returns' {
        foreach ($shape in Get-IisReturnShape -Kind ApplicationPool -Value 'Explicit') {
            $read = if ($shape -is [string]) { $shape } else { [string]$shape.Value }
            $read | Should -Be 'Explicit'
        }
    }

    It 'passes an empty value through, because that is a case the script has to refuse' {
        # Resolve-DeploymentTarget throws on a blank app pool name rather than
        # letting it flow into Stop-WebAppPool, and the test for that needs the
        # blank in both shapes like any other value.
        @(Get-IisReturnShape -Kind ApplicationPool -Value '').Count | Should -BeGreaterThan 1
    }
}

Describe 'the mock-shape contract' {

    BeforeAll {
        # Through Get-RepositoryPath rather than $PSScriptRoot, so the directory
        # this sweeps is a path the repository is checked to still have. A sweep
        # anchored to nothing finds nothing and passes.
        $script:SuiteRoot = Get-RepositoryPath 'Tests/PrTestEnvironments/Pester'
        $script:SuiteFiles = @(Get-ChildItem -Path $script:SuiteRoot -Filter $script:SuiteGlob -File)
    }

    It 'has suites to check' {
        $script:SuiteFiles.Count | Should -BeGreaterThan 0
    }

    It 'requires every suite that mocks <_> to take its shapes from Get-IisReturnShape' -ForEach (Get-ShapeVaryingCmdlet) {
        $cmdlet = $_
        $offenders = @()

        foreach ($file in $script:SuiteFiles) {
            $text = Get-Content -Raw -Path $file.FullName

            # Only suites that actually stub the cmdlet out. A suite that merely
            # asserts it was not invoked is making no claim about its shape.
            if ($text -notmatch "(?m)^\s*Mock\s+$([regex]::Escape($cmdlet))\s*\{") { continue }

            # A mock that throws is not claiming a return shape either -- it is
            # asserting the call never happens, as the DedicatedSite context does.
            $stubsAReturnValue = $text -match "(?m)^\s*Mock\s+$([regex]::Escape($cmdlet))\s*\{\s*(?!throw)"
            if (-not $stubsAReturnValue) { continue }

            if ($text -notmatch 'Get-IisReturnShape') {
                $offenders += $file.Name
            }
        }

        $offenders -join ', ' | Should -BeNullOrEmpty -Because "$cmdlet returns more than one shape, and a suite that hand-writes one of them is how the 2026-09-14 production failure stayed green"
    }
}
