<#
.SYNOPSIS
    Reports whether the deploy scripts published to the VMs match this commit.

.DESCRIPTION
    What a VM runs is not what is in this repository. Sync-DeploymentScripts
    refreshes C:\RockDeploy from the bootstrap prefix on every queue poll, and the
    only publisher to that prefix is pr-test-bootstrap-command-queue.yml, which is
    workflow_dispatch-only. So a fix merged to Deployment/** changes nothing on a
    VM until somebody remembers to dispatch a bootstrap -- and the deploys in
    between run the old script and report success, because the code that would
    have done the work was never there to fail. That is how an ACL fix sat merged
    for a day on 2026-08-19 while three green deploys kept serving a stale
    stylesheet.

    The bootstrap uploads Deployment/PrTestEnvironments/*.ps1 and
    Deployment/Database/*.ps1 into one flat prefix, so this compares both. That
    set includes Deploy-PrEnvironment.ps1, which is what the pr-* fleet runs, and
    Invoke-PrEnvironmentCommandQueue.ps1, which every VM runs. Nothing here is
    specific to one environment: the published set is shared, so the check is the
    same question whoever is deploying.

    This is a script rather than PowerShell inlined into action.yml so that the
    comparison can be loaded and executed by Pester. Sixty lines of YAML string
    had no test that ran a single branch of it -- including the branch that
    decides whether "in sync" is a true answer or a check that never ran. See
    Tests/PrTestEnvironments/Pester/ScriptDrift.Tests.ps1 and ADR-0002.

    This action cannot fail a deploy. It exists to make a problem visible and must
    never become one: a parse error or a gsutil outage here reports nothing and
    lets the deploy proceed, which is where every deploy stood before the check
    existed. That was `continue-on-error: true` on the caller's step while this
    lived in one workflow. It is enforced here instead, because a composite
    action's steps do not take that flag and because a second caller that forgot
    to copy it would turn a reporting step into a deploy blocker.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest

function Get-NormalizedHash {
    <#
    .SYNOPSIS
        SHA-256 of a file's text with line endings normalised to LF.
    .DESCRIPTION
        The two copies reach this runner through different Git checkouts, and
        PowerShell runs either -- so reporting a CRLF-only difference as drift
        would train people to ignore this warning, which is the one thing it
        cannot survive.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param([Parameter(Mandatory = $true)][string]$Path)

    $text = [System.IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString($sha.ComputeHash($bytes)).Replace("-", "")
    }
    finally {
        $sha.Dispose()
    }
}

function Get-ScriptDriftReport {
    <#
    .SYNOPSIS
        Compares local deploy scripts against the copies published to the VMs.
    .PARAMETER LocalDirectory
        Directories to sweep for *.ps1. Both of the bootstrap's sources, because
        checking only PrTestEnvironments would report "in sync" while a
        Deployment/Database script on the VM was a version behind -- and those are
        the ones an operator reaches for mid-cutover.
    .PARAMETER PublishedDirectory
        A directory holding the copies downloaded from the bootstrap prefix.
    .OUTPUTS
        Checked, Drifted and Missing. Checked is the count compared, and zero is
        its own answer rather than a clean one: see Get-DriftReportMessage.
    #>
    [CmdletBinding()]
    [OutputType([System.Collections.Specialized.OrderedDictionary])]
    param(
        [Parameter(Mandatory = $true)][string[]]$LocalDirectory,
        [Parameter(Mandatory = $true)][string]$PublishedDirectory
    )

    $localScripts = @( $LocalDirectory | ForEach-Object {
        Get-ChildItem -Path $_ -Filter *.ps1 -ErrorAction SilentlyContinue
    } )

    $drifted = @()
    $missing = @()
    foreach ($local in $localScripts) {
        $published = Join-Path $PublishedDirectory $local.Name
        if (-not (Test-Path $published)) {
            $missing += $local.Name
            continue
        }
        if ((Get-NormalizedHash -Path $local.FullName) -ne (Get-NormalizedHash -Path $published)) {
            $drifted += $local.Name
        }
    }

    return [ordered]@{
        Checked = $localScripts.Count
        Drifted = @($drifted | Sort-Object)
        Missing = @($missing | Sort-Object)
    }
}

function Get-DriftReportMessage {
    <#
    .SYNOPSIS
        Turns a drift report into what the log, the job summary and the warning say.
    .DESCRIPTION
        Three outcomes, and the first is the reason this is a function rather than
        two branches. Checking nothing and reporting "in sync" is the failure mode
        the whole check exists to prevent, so a report over zero files says the
        comparison did not run -- not that the VMs are current.
    .OUTPUTS
        Console lines, the job-summary row, and Warning, which is $null when there
        is nothing wrong.
    #>
    [CmdletBinding()]
    [OutputType([System.Collections.Specialized.OrderedDictionary])]
    param([Parameter(Mandatory = $true)]$Report)

    if ($Report.Checked -eq 0) {
        return [ordered]@{
            Console    = @()
            SummaryRow = "| Deploy scripts | not checked -- no local copy to compare |"
            Warning    = "Found no deploy scripts to compare, so this run could not check whether the VMs are up to date. The comparison, not the deploy, is what failed here."
        }
    }

    $stale = @($Report.Drifted) + @($Report.Missing)
    if ($stale.Count -eq 0) {
        return [ordered]@{
            Console    = @("Deploy scripts published to the VMs match this commit ($($Report.Checked) checked).")
            SummaryRow = "| Deploy scripts | in sync with this commit |"
            Warning    = $null
        }
    }

    $console = @()
    foreach ($name in $Report.Drifted) { $console += "  differs from the published copy: $name" }
    foreach ($name in $Report.Missing) { $console += "  never published: $name" }

    $names = $stale -join ", "
    return [ordered]@{
        Console    = $console
        SummaryRow = "| Deploy scripts | STALE ON THE VM -- $names |"
        Warning    = "The deploy scripts on the VM are not the ones in this commit ($names). This deploy will run the published copies and will report success either way, so a fix you expect to be exercised here may not run at all. Dispatch the 'PR Test - Bootstrap command queue' workflow to publish them, then deploy again."
    }
}

# Dot-sourcing this file to test the functions must not run the download.
# action.yml sets DRIFT_INVOKED to a literal; a Pester run does not.
#
# Deliberately not DRIFT_BUCKET. Guarding on an input the caller supplies means a
# workflow that resolves `bucket` to an empty string gets a step that silently
# reports nothing, which is indistinguishable from the answer this check exists to
# give. With the marker, a blank bucket reaches the comparison and is reported as
# a check that could not run.
if ([string]::IsNullOrWhiteSpace($env:DRIFT_INVOKED)) {
    return
}

# Everything below is inside one trap. This step is structural: it makes a problem
# visible and must never become one. See the header.
try {
    $localDirectories = @( "Deployment/PrTestEnvironments", "Deployment/Database" )
    $publishedPrefix = "gs://$env:DRIFT_BUCKET/pr-environments/bootstrap/latest"
    $downloadDirectory = Join-Path $env:RUNNER_TEMP "published-deploy-scripts"
    New-Item -ItemType Directory -Force -Path $downloadDirectory | Out-Null

    # Failure here is not fatal and must not read as "in sync": if the copy returns
    # nothing, every script falls into Missing and the warning says so.
    gsutil -m cp "$publishedPrefix/*.ps1" $downloadDirectory 2>&1 | Out-Null

    $report = Get-ScriptDriftReport -LocalDirectory $localDirectories -PublishedDirectory $downloadDirectory
    $message = Get-DriftReportMessage -Report $report

    foreach ($line in $message.Console) { Write-Host $line }
    $message.SummaryRow | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append
    if ($null -ne $message.Warning) { Write-Host "::warning::$($message.Warning)" }
}
catch {
    Write-Host "::warning::The deploy script drift check did not run: $($_.Exception.Message). The deploy is unaffected -- this step only reports."
}

exit 0
