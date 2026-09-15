<#
    What a VM runs is not what is in this repository. The queue agent syncs
    C:\RockDeploy from a bootstrap prefix that only a workflow_dispatch publishes
    to, so a fix merged to Deployment/** changes nothing on the box until somebody
    remembers to dispatch one -- and the deploys in between run the old script and
    report success, because the code that would have done the work was never there
    to fail. An ACL fix sat merged for a day on 2026-08-19 while three green
    deploys kept serving a stale stylesheet.

    The comparison that catches this lived as sixty lines of PowerShell inside a
    YAML string, which nothing could execute. The branch that matters most was the
    one nobody could reach: a comparison that finds no local scripts must say the
    check did not run, because reporting "in sync" against nothing is the exact
    failure this check exists to prevent. That branch now has a test, which is the
    whole reason the script moved out of the workflow.

    The other branch worth naming is line endings. The two copies reach the runner
    through different Git checkouts and PowerShell runs either, so a CRLF-only
    difference reported as drift would train people to ignore the warning -- and
    this warning cannot survive being ignored.
#>

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'ScriptFunctions.psm1') -Force

    $script:ActionScript = Get-RepositoryPath '.github/actions/report-script-drift/Get-ScriptDrift.ps1'
    . (Import-ScriptFunction -Path $script:ActionScript -Name 'Get-NormalizedHash', 'Get-ScriptDriftReport', 'Get-DriftReportMessage')
}

Describe 'Get-NormalizedHash' {
    BeforeEach {
        $script:Root = Join-Path $TestDrive ('hash-' + [guid]::NewGuid().ToString('n'))
        New-Item -ItemType Directory -Path $script:Root -Force | Out-Null
    }

    It 'reads the same file the same way whichever line ending it arrived with' {
        $lf = Join-Path $script:Root 'lf.txt'
        $crlf = Join-Path $script:Root 'crlf.txt'
        [System.IO.File]::WriteAllText($lf, "Write-Host one`nWrite-Host two`n")
        [System.IO.File]::WriteAllText($crlf, "Write-Host one`r`nWrite-Host two`r`n")

        Get-NormalizedHash -Path $crlf | Should -Be (Get-NormalizedHash -Path $lf)
    }

    It 'separates files whose content actually differs' {
        $first = Join-Path $script:Root 'first.txt'
        $second = Join-Path $script:Root 'second.txt'
        [System.IO.File]::WriteAllText($first, "Write-Host one`n")
        [System.IO.File]::WriteAllText($second, "Write-Host two`n")

        Get-NormalizedHash -Path $second | Should -Not -Be (Get-NormalizedHash -Path $first)
    }
}

Describe 'Get-ScriptDriftReport' {
    BeforeEach {
        $script:Root = Join-Path $TestDrive ('drift-' + [guid]::NewGuid().ToString('n'))
        $script:EnvironmentScripts = Join-Path $script:Root 'PrTestEnvironments'
        $script:DatabaseScripts = Join-Path $script:Root 'Database'
        $script:Published = Join-Path $script:Root 'published'
        foreach ($directory in @($script:EnvironmentScripts, $script:DatabaseScripts, $script:Published)) {
            New-Item -ItemType Directory -Path $directory -Force | Out-Null
        }

        $script:LocalDirectories = @($script:EnvironmentScripts, $script:DatabaseScripts)

        # Real script names, taken from the tree rather than invented. The suite
        # scanner in test_powershell_job.py reads a quoted *.ps1 literal as a
        # script this suite claims to test, and it is right to: a fixture named
        # after nothing is a fixture that stops matching the thing it stands for.
        $script:DatabaseScript = Split-Path -Leaf (Get-RepositoryPath 'Deployment/Database/Set-RockThemeCustomization.ps1')
        $script:UnpublishedScript = Split-Path -Leaf (Get-RepositoryPath 'Deployment/PrTestEnvironments/Stop-PrEnvironment.ps1')
    }

    It 'reports nothing stale when every published copy matches' {
        [System.IO.File]::WriteAllText((Join-Path $script:EnvironmentScripts 'Deploy-PrEnvironment.ps1'), "Write-Host deploy`n")
        [System.IO.File]::WriteAllText((Join-Path $script:Published 'Deploy-PrEnvironment.ps1'), "Write-Host deploy`n")

        $report = Get-ScriptDriftReport -LocalDirectory $script:LocalDirectories -PublishedDirectory $script:Published

        $report.Checked | Should -Be 1
        $report.Drifted | Should -BeNullOrEmpty
        $report.Missing | Should -BeNullOrEmpty
    }

    It 'names a script whose published copy is a version behind' {
        [System.IO.File]::WriteAllText((Join-Path $script:EnvironmentScripts 'Deploy-PrEnvironment.ps1'), "Write-Host fixed`n")
        [System.IO.File]::WriteAllText((Join-Path $script:Published 'Deploy-PrEnvironment.ps1'), "Write-Host stale`n")

        $report = Get-ScriptDriftReport -LocalDirectory $script:LocalDirectories -PublishedDirectory $script:Published

        $report.Drifted | Should -Be @('Deploy-PrEnvironment.ps1')
        $report.Missing | Should -BeNullOrEmpty
    }

    It 'separates never-published from drifted, because the fix differs' {
        [System.IO.File]::WriteAllText((Join-Path $script:EnvironmentScripts $script:UnpublishedScript), "Write-Host new`n")

        $report = Get-ScriptDriftReport -LocalDirectory $script:LocalDirectories -PublishedDirectory $script:Published

        $report.Missing | Should -Be @($script:UnpublishedScript)
        $report.Drifted | Should -BeNullOrEmpty
    }

    It 'does not call a CRLF-only difference drift' {
        [System.IO.File]::WriteAllText((Join-Path $script:EnvironmentScripts 'Deploy-PrEnvironment.ps1'), "Write-Host deploy`n")
        [System.IO.File]::WriteAllText((Join-Path $script:Published 'Deploy-PrEnvironment.ps1'), "Write-Host deploy`r`n")

        $report = Get-ScriptDriftReport -LocalDirectory $script:LocalDirectories -PublishedDirectory $script:Published

        $report.Checked | Should -Be 1
        $report.Drifted | Should -BeNullOrEmpty
    }

    It 'sweeps both directories, because the bootstrap uploads both into one prefix' {
        # Checking only PrTestEnvironments would report "in sync" while a
        # Deployment/Database script on the VM was a version behind -- and those
        # are the ones an operator reaches for mid-cutover.
        [System.IO.File]::WriteAllText((Join-Path $script:EnvironmentScripts 'Deploy-PrEnvironment.ps1'), "Write-Host deploy`n")
        [System.IO.File]::WriteAllText((Join-Path $script:Published 'Deploy-PrEnvironment.ps1'), "Write-Host deploy`n")
        [System.IO.File]::WriteAllText((Join-Path $script:DatabaseScripts $script:DatabaseScript), "Write-Host fixed`n")
        [System.IO.File]::WriteAllText((Join-Path $script:Published $script:DatabaseScript), "Write-Host stale`n")

        $report = Get-ScriptDriftReport -LocalDirectory $script:LocalDirectories -PublishedDirectory $script:Published

        $report.Checked | Should -Be 2
        $report.Drifted | Should -Be @($script:DatabaseScript)
    }

    It 'survives a local directory that is not there' {
        # The caller's sparse-checkout decides whether these exist, and an action
        # cannot widen it. A checkout that dropped one must not throw here: this
        # step reports, and a throw would be the reporting step failing a deploy.
        $report = Get-ScriptDriftReport -LocalDirectory @((Join-Path $script:Root 'absent')) -PublishedDirectory $script:Published

        $report.Checked | Should -Be 0
    }
}

Describe 'Get-DriftReportMessage' {
    It 'says the check did not run rather than reporting a clean result over nothing' {
        # The branch this whole suite exists for. A sparse-checkout that stopped
        # bringing the scripts down leaves Checked at zero, and "in sync" would be
        # the one answer that is both wrong and reassuring.
        $message = Get-DriftReportMessage -Report ([ordered]@{ Checked = 0; Drifted = @(); Missing = @() })

        $message.Warning | Should -Not -BeNullOrEmpty
        $message.Warning | Should -BeLike '*could not check*'
        $message.SummaryRow | Should -BeLike '*not checked*'
        $message.SummaryRow | Should -Not -BeLike '*in sync*'
    }

    It 'warns about nothing when every script matches' {
        $message = Get-DriftReportMessage -Report ([ordered]@{ Checked = 4; Drifted = @(); Missing = @() })

        $message.Warning | Should -BeNullOrEmpty
        $message.SummaryRow | Should -BeLike '*in sync with this commit*'
    }

    It 'names the stale scripts in the warning, so the log says which' {
        $message = Get-DriftReportMessage -Report ([ordered]@{ Checked = 3; Drifted = @('Deploy-PrEnvironment.ps1'); Missing = @() })

        $message.Warning | Should -BeLike '*Deploy-PrEnvironment.ps1*'
        $message.SummaryRow | Should -BeLike '*STALE ON THE VM*'
    }

    It 'tells an operator what to do about it' {
        # A warning that names a problem and not its fix gets read once.
        $message = Get-DriftReportMessage -Report ([ordered]@{ Checked = 3; Drifted = @('Deploy-PrEnvironment.ps1'); Missing = @() })

        $message.Warning | Should -BeLike '*Bootstrap command queue*'
    }

    It 'distinguishes the two kinds of stale in the console lines' {
        # Two different fixes behind one warning: a drifted script needs a
        # bootstrap dispatch, and a never-published one needs somebody to notice it
        # was added and never sent.
        $drifted = 'Deploy-PrEnvironment.ps1'
        $missing = Split-Path -Leaf (Get-RepositoryPath 'Deployment/PrTestEnvironments/Stop-PrEnvironment.ps1')

        $message = Get-DriftReportMessage -Report ([ordered]@{
            Checked = 5
            Drifted = @($drifted)
            Missing = @($missing)
        })

        ($message.Console -join "`n") | Should -BeLike "*differs from the published copy: $drifted*"
        ($message.Console -join "`n") | Should -BeLike "*never published: $missing*"
        $message.Warning | Should -BeLike "*$drifted, $missing*"
    }
}
