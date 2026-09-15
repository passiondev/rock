<#
.SYNOPSIS
    Stops a VM, optionally changes its service account scopes while it is down,
    and starts it again with a bounded retry.

.DESCRIPTION
    Three workflows staged a startup script and then rebooted to run it, and each
    spelled the reboot itself a little differently. The pr-test bootstrap checked
    neither the stop nor the scope change, so a refused stop read as a green
    bootstrap whose agent never installed; the diagnose run checked the stop and
    not the scopes; only production checked both. The retry loop around `start`
    was the same six attempts in all three, copied.

    The scopes are an input and not a constant because deciding *which* scopes an
    instance should carry is the caller's policy: the pr-test fleet replaces the
    list with cloud-platform, and production reads its current scopes, refuses to
    continue if the read looks implausible, and unions rather than replaces. That
    decision has to happen before the stop -- there is nothing to recover to once
    the instance is down -- so it stays in the workflow that makes it. What is
    shared here is the mechanism underneath: stop, apply, start, and say what is
    down if it never comes back.

    production-bootstrap-command-queue.yml is deliberately not a caller, and
    ADR-0008 is where that is written down. Its restart is on the path that
    touches production on upgrade day, and its pre-stop validation and its
    recovery are one sequence with the scope policy above them rather than a
    caller of this. Routing it here to save lines would put a shared edit on
    production's only bootstrap.

    This is a script rather than PowerShell inlined into action.yml for the reason
    in ADR-0002: PowerShell embedded in YAML is a string that nothing can run.
    The two functions below are the parts with a wrong answer available to them,
    and Tests/PrTestEnvironments/Pester/RestartVm.Tests.ps1 executes them.

    Kept to Windows PowerShell 5.1 syntax, matching its siblings.
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Get-ServiceAccountScopeArgument {
    <#
    .SYNOPSIS
        The `--scopes=` argument for a requested scope list, or $null to leave the
        instance's service account alone.
    .DESCRIPTION
        $null and not an empty argument. `set-service-account --scopes=` is not a
        no-op -- it is accepted and strips the instance of every scope, which is
        the same shape of failure as the blank external IP in resolve-vm: an unset
        input asking gcloud to act on nothing and gcloud finding something to do.
        A caller that wants no scope change passes nothing, and no
        set-service-account call is made at all.

        Blank entries are dropped and duplicates collapse, because the callers
        build this list by splitting and appending rather than by writing it out.
    .OUTPUTS
        `--scopes=a,b` or $null.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][AllowNull()][string]$Scopes
    )

    if ([string]::IsNullOrWhiteSpace($Scopes)) {
        return $null
    }

    $wanted = @($Scopes -split '[,;\s]+' | Where-Object { $_ } | Select-Object -Unique)
    if ($wanted.Count -eq 0) {
        return $null
    }

    return "--scopes=$($wanted -join ',')"
}

function Get-RestartFailureMessage {
    <#
    .SYNOPSIS
        What to throw when the instance will not start again.
    .DESCRIPTION
        The caller supplies the sentence naming what is down, because only the
        caller knows. The same failed `start` leaves the pr-test box carrying
        staging and the whole pr-* fleet, or leaves production offline, and an
        operator reading the run needs to be told which. A caller that passes
        nothing gets a refusal rather than a generic message on a fleet-down run.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)][string]$VmName,
        [Parameter(Mandatory = $true)][string]$Zone,
        [Parameter(Mandatory = $true)][int]$Attempts,
        [Parameter(Mandatory = $true)][AllowEmptyString()][AllowNull()][string]$DownMessage
    )

    if ([string]::IsNullOrWhiteSpace($DownMessage)) {
        throw "Get-RestartFailureMessage needs a DownMessage naming what is offline; '$VmName' alone does not tell an operator what to go and start."
    }

    return "Failed to start VM $VmName in $Zone after $Attempts attempts. $($DownMessage.Trim())"
}

if ([string]::IsNullOrWhiteSpace($env:RESTART_INVOKED)) { return }

$vmName = $env:RESTART_VM_NAME
$vmZone = $env:RESTART_VM_ZONE
$attempts = 6

# Built into a variable first, and splatted as an array below. A parenthesised
# expression inside a native command's argument list is passed through as literal
# text rather than evaluated -- see the same note in resolve-vm.
$scopeArgument = Get-ServiceAccountScopeArgument -Scopes $env:RESTART_SCOPES

Write-Host "Stopping $vmName in $vmZone."
gcloud compute instances stop $vmName --zone=$vmZone
if ($LASTEXITCODE -ne 0) {
    throw "Failed to stop $vmName in $vmZone, so the staged startup script never ran and nothing was applied."
}

if ($null -ne $scopeArgument) {
    Write-Host "Setting service account scopes: $scopeArgument"
    gcloud compute instances set-service-account $vmName --zone=$vmZone $scopeArgument
    if ($LASTEXITCODE -ne 0) {
        # The instance is stopped at this point, so it gets started again before
        # this throws. Leaving it down would turn a refused scope change into an
        # outage that needs a hand on the console.
        Write-Warning "Could not set the service account scopes. Starting $vmName again before failing."
        gcloud compute instances start $vmName --zone=$vmZone
        throw "Failed to set service account scopes on $vmName."
    }
}

Start-Sleep -Seconds 10

$started = $false
for ($attempt = 1; $attempt -le $attempts; $attempt++) {
    gcloud compute instances start $vmName --zone=$vmZone
    if ($LASTEXITCODE -eq 0) {
        $started = $true
        break
    }
    Write-Warning "VM start attempt $attempt failed; retrying in 30 seconds."
    Start-Sleep -Seconds 30
}

if (-not $started) {
    throw (Get-RestartFailureMessage -VmName $vmName -Zone $vmZone -Attempts $attempts -DownMessage $env:RESTART_DOWN_MESSAGE)
}

Write-Host "$vmName is running. The staged startup script ran at boot."
