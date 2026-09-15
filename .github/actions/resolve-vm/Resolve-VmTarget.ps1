<#
.SYNOPSIS
    Resolves the PR environment VM to a name and a bare zone, by external IP with
    a configured fallback.

.DESCRIPTION
    Three workflows reach the VM without going through the command queue -- the
    bootstrap that installs the queue agent, the certificate renewal, and the
    diagnose run -- and each one has to turn an external IP into a name and zone
    before it can call gcloud. All three carried their own copy, and the copies
    had drifted in two ways that no review of a single file would surface:

    1. The filter. Two spell it `networkInterfaces.accessConfigs.natIP` and one
       spells it `networkInterfaces.accessConfigs[0].natIP`. They agree only
       while every instance has its external IP on the first access config, which
       is true today and is a property of the fleet rather than of the lookup.

    2. The guard. Only the certificate renewal checked that anything was actually
       resolved. The other two carried an unresolved name straight into
       `gcloud compute instances add-metadata` -- and in the diagnose run's case,
       into a stop and a start.

    This is a script rather than PowerShell inlined into action.yml so that Pester
    can execute the parsing and the fallback. PowerShell embedded in YAML is a
    string that nothing runs until a runner expands it, which is how three copies
    of nine lines came to disagree. See
    Tests/PrTestEnvironments/Pester/ResolveVmTarget.Tests.ps1.
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-VmLookupFilter {
    <#
    .SYNOPSIS
        The gcloud filter that finds the instance answering on an external IP.

    .DESCRIPTION
        `accessConfigs.natIP`, not `accessConfigs[0].natIP`. Both forms are valid
        and today they return the same instance, so this is a choice between two
        working spellings rather than a fix.

        The unindexed form matches when any access config on any interface carries
        the address, which is the question being asked: which instance answers on
        this IP. The indexed form asks a narrower question -- does the first
        access config carry it -- and reads as though the index were load-bearing
        when it is the accident that the fleet is single-homed.

        Returns $null rather than a filter when there is no IP to look up. An
        unset GCP_VM_EXTERNAL_IP would otherwise interpolate to `natIP=`, which is
        not a filter that matches nothing -- it matches every instance with no
        external address, and the first of those becomes the VM this run acts on.
        The caller skips the lookup on $null and uses its configured fallback,
        which is the answer an absent IP should give.
    .OUTPUTS
        The filter string, or $null when no lookup should be attempted.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][AllowNull()][string]$ExternalIp
    )

    if ([string]::IsNullOrWhiteSpace($ExternalIp)) {
        return $null
    }

    return "networkInterfaces.accessConfigs.natIP=$($ExternalIp.Trim())"
}

function ConvertFrom-VmListRow {
    <#
    .SYNOPSIS
        Parses one `csv[no-heading](name,zone)` row into a name and a bare zone.

    .DESCRIPTION
        gcloud returns the zone as a full resource URL
        (`https://.../zones/us-east1-b`) and every caller of gcloud wants the last
        segment, so the split belongs here rather than in three workflows.

        A blank row is the normal no-match answer from `instances list`, not an
        error: the caller falls back to its configured name and zone. Returns
        $null for it so that the fallback is a branch on a value rather than on a
        string-emptiness test repeated at each site.
    .OUTPUTS
        An ordered dictionary with Name and Zone, or $null when the row is blank
        or does not carry both fields.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][AllowNull()][string]$Row
    )

    if ([string]::IsNullOrWhiteSpace($Row)) {
        return $null
    }

    $parts = $Row.Split(',')
    if ($parts.Count -lt 2) {
        return $null
    }

    $name = $parts[0].Trim()
    # Last segment, so a bare zone passes through unchanged and a resource URL is
    # reduced to the same thing. gcloud accepts only the bare form for --zone.
    $zone = ($parts[1].Trim() -split '/')[-1]

    if ([string]::IsNullOrWhiteSpace($name) -or [string]::IsNullOrWhiteSpace($zone)) {
        return $null
    }

    return [ordered]@{
        Name = $name
        Zone = $zone
    }
}

function Resolve-VmTarget {
    <#
    .SYNOPSIS
        Picks the VM to act on: the one found by IP, else the configured one.

    .DESCRIPTION
        The lookup wins over the configured values because the IP is the thing an
        operator can see answering, while GCP_VM_NAME and GCP_ZONE are secrets set
        once and outlive a rebuild of the instance.

        Both halves have to produce a name and a zone together. A resolved name
        with someone else's zone is worse than no answer: gcloud reports that the
        instance does not exist in that zone, which reads as a deleted VM.

        Throwing when neither resolves is the behaviour the certificate renewal
        had and the other two callers did not. Without it an empty name reaches
        gcloud, and for the diagnose run that means a stop and a start aimed at
        nothing, reported as a gcloud usage error several steps from its cause.
    .OUTPUTS
        An ordered dictionary with Name, Zone, and Source -- 'lookup' or
        'configured' -- so the caller can say in its log which one it acted on.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][AllowEmptyString()][AllowNull()][string]$Row,
        [Parameter(Mandatory = $false)][AllowEmptyString()][string]$FallbackName = '',
        [Parameter(Mandatory = $false)][AllowEmptyString()][string]$FallbackZone = ''
    )

    $found = ConvertFrom-VmListRow -Row $Row
    if ($null -ne $found) {
        return [ordered]@{
            Name   = $found.Name
            Zone   = $found.Zone
            Source = 'lookup'
        }
    }

    if ([string]::IsNullOrWhiteSpace($FallbackName) -or [string]::IsNullOrWhiteSpace($FallbackZone)) {
        throw "Could not resolve the PR environment VM. No instance answers on the configured external IP, and GCP_VM_NAME and GCP_ZONE are not both set."
    }

    return [ordered]@{
        Name   = $FallbackName
        Zone   = ($FallbackZone -split '/')[-1]
        Source = 'configured'
    }
}

# Dot-sourcing this file to test the functions must not run the lookup. action.yml
# sets RVM_INVOKED to a literal; a Pester run does not. Guarding on a caller-supplied
# input instead would mean a workflow that resolved one to an empty string got a
# step that exited 0 having resolved nothing. See the sibling note in
# queue-vm-command/action.yml.
if ([string]::IsNullOrWhiteSpace($env:RVM_INVOKED)) {
    return
}

# Built into a variable first. A parenthesised expression inside a native
# command's argument is passed through as literal text rather than evaluated, so
# `--filter=(Get-VmLookupFilter ...)` would ask gcloud to match on the function
# call itself -- which returns no instances and looks exactly like a VM that is
# gone.
$filter = Get-VmLookupFilter -ExternalIp $env:RVM_EXTERNAL_IP

$row = ''
if ($null -ne $filter) {
    $row = gcloud compute instances list `
        --filter=$filter `
        --format="csv[no-heading](name,zone)" | Select-Object -First 1
}

$target = Resolve-VmTarget -Row $row -FallbackName $env:RVM_FALLBACK_NAME -FallbackZone $env:RVM_FALLBACK_ZONE

Write-Host "VM $($target.Name) in zone $($target.Zone), resolved by $($target.Source)."

"name=$($target.Name)" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
"zone=$($target.Zone)" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
