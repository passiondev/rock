BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'ScriptFunctions.psm1') -Force
    $script:ActionScript = Get-RepositoryPath '.github/actions/resolve-vm/Resolve-VmTarget.ps1'
    . (Import-ScriptFunction -Path $script:ActionScript -Name 'Get-VmLookupFilter', 'ConvertFrom-VmListRow', 'Resolve-VmTarget')
}

Describe 'Get-VmLookupFilter' {
    It 'asks which instance answers on the address, not which one answers on it first' {
        # The drift this action exists to remove. One of the three callers spelled
        # it `accessConfigs[0].natIP`, and the three agreed only because every
        # instance in the fleet has its address on the first access config.
        $filter = Get-VmLookupFilter -ExternalIp '203.0.113.7'

        $filter | Should -Be 'networkInterfaces.accessConfigs.natIP=203.0.113.7'
        $filter | Should -Not -Match '\['
    }

    It 'refuses to build a filter when there is no address to look up' {
        # `natIP=` is a filter that matches every instance with no external
        # address rather than no instance at all, so an unset secret would pick a
        # VM rather than fall back to the configured one.
        Get-VmLookupFilter -ExternalIp '' | Should -BeNullOrEmpty
        Get-VmLookupFilter -ExternalIp '   ' | Should -BeNullOrEmpty
        Get-VmLookupFilter -ExternalIp $null | Should -BeNullOrEmpty
    }
}

Describe 'ConvertFrom-VmListRow' {
    It 'reduces the zone resource URL gcloud returns to the bare zone' {
        $parsed = ConvertFrom-VmListRow -Row 'connect-srv-test,https://www.googleapis.com/compute/v1/projects/p/zones/us-east1-b'

        $parsed.Name | Should -Be 'connect-srv-test'
        $parsed.Zone | Should -Be 'us-east1-b'
    }

    It 'passes a bare zone through unchanged' {
        (ConvertFrom-VmListRow -Row 'vm-a,us-central1-a').Zone | Should -Be 'us-central1-a'
    }

    It 'reads no match as no answer rather than as an error' {
        # An empty row is what `instances list` returns for an IP nothing answers
        # on. It is the ordinary case that hands over to the fallback.
        ConvertFrom-VmListRow -Row '' | Should -BeNullOrEmpty
        ConvertFrom-VmListRow -Row '   ' | Should -BeNullOrEmpty
        ConvertFrom-VmListRow -Row $null | Should -BeNullOrEmpty
    }

    It 'refuses a row that does not carry both fields' {
        # Half an answer is worse than none: a name without a zone sends gcloud
        # looking in the wrong place and it reports a live VM as nonexistent.
        ConvertFrom-VmListRow -Row 'vm-a' | Should -BeNullOrEmpty
        ConvertFrom-VmListRow -Row 'vm-a,' | Should -BeNullOrEmpty
        ConvertFrom-VmListRow -Row ',us-east1-b' | Should -BeNullOrEmpty
    }

    It 'trims the whitespace a csv row can carry' {
        $parsed = ConvertFrom-VmListRow -Row '  vm-a , us-east1-b  '

        $parsed.Name | Should -Be 'vm-a'
        $parsed.Zone | Should -Be 'us-east1-b'
    }
}

Describe 'Resolve-VmTarget' {
    It 'prefers the instance found by address over the configured one' {
        # The address is what an operator can see answering. The configured name
        # and zone are secrets set once, and they outlive a rebuild of the VM.
        $target = Resolve-VmTarget -Row 'found-vm,https://example/zones/us-east1-b' -FallbackName 'configured-vm' -FallbackZone 'us-west1-a'

        $target.Name | Should -Be 'found-vm'
        $target.Zone | Should -Be 'us-east1-b'
        $target.Source | Should -Be 'lookup'
    }

    It 'falls back to the configured VM when nothing answers on the address' {
        $target = Resolve-VmTarget -Row '' -FallbackName 'configured-vm' -FallbackZone 'us-west1-a'

        $target.Name | Should -Be 'configured-vm'
        $target.Zone | Should -Be 'us-west1-a'
        $target.Source | Should -Be 'configured'
    }

    It 'reduces a configured zone URL to the bare zone as well' {
        # gcloud takes only the bare form for --zone, and the fallback comes from a
        # secret somebody typed rather than from gcloud's own output.
        (Resolve-VmTarget -Row '' -FallbackName 'vm' -FallbackZone 'https://example/zones/us-west1-a').Zone | Should -Be 'us-west1-a'
    }

    It 'says which VM it picked so the log records it' {
        (Resolve-VmTarget -Row 'a,b' -FallbackName 'c' -FallbackZone 'd').Source | Should -Be 'lookup'
        (Resolve-VmTarget -Row '' -FallbackName 'c' -FallbackZone 'd').Source | Should -Be 'configured'
    }

    It 'throws rather than hand an empty name to gcloud' {
        # This is the guard only the certificate renewal had. Without it the
        # diagnose run aims a stop and a start at nothing, and reports it as a
        # gcloud usage error several steps away from the cause.
        { Resolve-VmTarget -Row '' -FallbackName '' -FallbackZone '' } | Should -Throw -ExpectedMessage '*Could not resolve*'
    }

    It 'treats half a fallback as no fallback' {
        { Resolve-VmTarget -Row '' -FallbackName 'vm' -FallbackZone '' } | Should -Throw -ExpectedMessage '*Could not resolve*'
        { Resolve-VmTarget -Row '' -FallbackName '' -FallbackZone 'us-west1-a' } | Should -Throw -ExpectedMessage '*Could not resolve*'
    }
}
