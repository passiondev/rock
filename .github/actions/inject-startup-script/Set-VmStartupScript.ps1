<#
.SYNOPSIS
    Validates a generated Windows startup script and stages it on a VM's metadata.

.DESCRIPTION
    Three workflows reach the VM by writing PowerShell into its
    `windows-startup-script-ps1` metadata and letting it run at the next boot --
    the PR fleet's bootstrap, production's bootstrap, and the diagnose run. It is
    the second control plane onto these boxes, alongside the command queue, and it
    had no module and no name.

    Each caller built its script as a here-string in YAML, wrote it to a temp file
    and called `add-metadata`. Two of the three never looked at whether gcloud
    accepted it. None of the three looked at what they were staging.

    That last gap is the reason this is worth a module rather than a tidy-up. A
    startup script is the one payload nothing downstream validates: it is a
    metadata string until the machine reboots, and then it runs unattended on a box
    with no console anybody is watching. A here-string that interpolated to
    nothing, or to PowerShell that does not parse, stages successfully, boots
    successfully, installs nothing, and reports success. For production's bootstrap
    that is an upgrade-day agent that was never there.

    So the payload is parsed here, before it is staged, for the same reason the
    fleet bootstrap parses the deployment scripts before publishing them.

    The caller still generates its own script and writes it to disk. Only the
    validation and the staging live here: passing several hundred lines of
    generated PowerShell back out through a YAML action input would put backticks
    and `$` through another round of expansion, which is a fine way to corrupt the
    exact payload this is meant to protect.

    This is a script rather than PowerShell inlined into action.yml so that Pester
    can execute the rejection rules -- see ADR-0002 and
    Tests/PrTestEnvironments/Pester/VmStartupScript.Tests.ps1.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-StartupScript {
    <#
    .SYNOPSIS
        Reads a generated startup script and reports what is wrong with it.

    .DESCRIPTION
        Separated from the decision about what to do so that the decision is a pure
        function over a small record, and so a caller reading the log can see the
        length that was staged rather than inferring it from success.
    .OUTPUTS
        An ordered dictionary with Exists, Length and ParseErrors.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path -LiteralPath $Path)) {
        return [ordered]@{
            Exists      = $false
            IsBlank     = $true
            Length      = 0
            ParseErrors = @()
        }
    }

    $text = [System.IO.File]::ReadAllText($Path)

    # Parsed from the text rather than the path. ParseFile would read the file a
    # second time, and the length reported below has to be the length of the thing
    # that was parsed or the two halves can disagree.
    $parseErrors = $null
    $null = [System.Management.Automation.Language.Parser]::ParseInput($text, [ref]$null, [ref]$parseErrors)

    return [ordered]@{
        Exists      = $true
        IsBlank     = [string]::IsNullOrWhiteSpace($text)
        Length      = $text.Length
        ParseErrors = @($parseErrors)
    }
}

function Get-StartupScriptRejection {
    <#
    .SYNOPSIS
        The reason not to stage this script, or $null when it is safe to stage.

    .DESCRIPTION
        Every message names the boot as the place the damage would show up, because
        that is what makes these failures expensive: staging is silent, the reboot
        is minutes later, and the machine comes up having run nothing.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]$Report,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Path
    )

    if (-not $Report.Exists) {
        return "No startup script was written to $Path. Staging nothing would leave the VM booting the previous script, or none at all."
    }

    # Whitespace counts as empty, and this is the order that matters: a blank file
    # parses cleanly, so the parse check below would pass it. A here-string whose
    # every interpolation resolved to nothing still produces newlines and
    # indentation, which is the shape this failure actually arrives in.
    if ($Report.IsBlank) {
        return "The startup script at $Path is empty. It would stage and boot successfully while installing nothing."
    }

    if ($Report.ParseErrors.Count -gt 0) {
        $first = $Report.ParseErrors[0]
        return "The startup script at $Path does not parse as PowerShell: $($first.Message) A startup script is not checked by anything between here and the boot, so this would stage, reboot, and run nothing."
    }

    return $null
}

# Dot-sourcing this file to test the functions must not stage anything. action.yml
# sets VSS_INVOKED to a literal; a Pester run does not. See the sibling note in
# queue-vm-command/action.yml for why the guard reads a marker rather than one of
# the caller's own inputs.
if ([string]::IsNullOrWhiteSpace($env:VSS_INVOKED)) {
    return
}

$scriptPath = $env:VSS_SCRIPT_PATH
$report = Test-StartupScript -Path $scriptPath
$rejection = Get-StartupScriptRejection -Report $report -Path $scriptPath

if ($null -ne $rejection) {
    throw $rejection
}

Write-Host "Staging a $($report.Length)-character startup script on $($env:VSS_VM_NAME) in $($env:VSS_VM_ZONE)."

# Staging is not itself a restart: the script takes effect at the next boot, and
# nothing here causes one. The caller decides when, and for production that is a
# separate step behind its own input.
gcloud compute instances add-metadata $env:VSS_VM_NAME `
    --zone=$env:VSS_VM_ZONE `
    --metadata-from-file=windows-startup-script-ps1=$scriptPath

if ($LASTEXITCODE -ne 0) {
    throw "Failed to stage the startup script on $($env:VSS_VM_NAME). The VM still has whatever was there before, so a reboot now would run the old script."
}

Write-Host "Staged. It runs at the next boot of $($env:VSS_VM_NAME)."
