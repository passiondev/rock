BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'ScriptFunctions.psm1') -Force
    $script:ActionScript = Get-RepositoryPath '.github/actions/restart-vm/Restart-Vm.ps1'
    . (Import-ScriptFunction -Path $script:ActionScript -Name 'Get-ServiceAccountScopeArgument', 'Get-RestartFailureMessage')
}

Describe 'Get-ServiceAccountScopeArgument' {
    It 'returns nothing when no scopes are requested, so set-service-account is not called at all' {
        # The bug this exists to stop: `--scopes=` is accepted by gcloud and strips
        # the instance of every scope. An unset input has to mean "make no call",
        # not "make the call with an empty list".
        Get-ServiceAccountScopeArgument -Scopes '' | Should -BeNullOrEmpty
        Get-ServiceAccountScopeArgument -Scopes '   ' | Should -BeNullOrEmpty
        Get-ServiceAccountScopeArgument -Scopes $null | Should -BeNullOrEmpty
    }

    It 'builds the flag from a single scope' {
        Get-ServiceAccountScopeArgument -Scopes 'https://www.googleapis.com/auth/cloud-platform' |
            Should -Be '--scopes=https://www.googleapis.com/auth/cloud-platform'
    }

    It 'joins several scopes with commas' {
        Get-ServiceAccountScopeArgument -Scopes 'a,b,c' | Should -Be '--scopes=a,b,c'
    }

    It 'drops the empty entries a split-and-append leaves behind' {
        # Production builds its list by splitting the current scopes and appending,
        # which is how a trailing separator gets here.
        Get-ServiceAccountScopeArgument -Scopes 'a,,b,' | Should -Be '--scopes=a,b'
    }

    It 'accepts the separators the callers actually produce' {
        Get-ServiceAccountScopeArgument -Scopes "a;b`n c" | Should -Be '--scopes=a,b,c'
    }

    It 'collapses a scope named twice' {
        Get-ServiceAccountScopeArgument -Scopes 'a,b,a' | Should -Be '--scopes=a,b'
    }

    It 'returns nothing for a list that is only separators' {
        # Not `--scopes=`, which would strip the instance. Same answer as a blank
        # input, because it carries the same amount of information.
        Get-ServiceAccountScopeArgument -Scopes ',,,' | Should -BeNullOrEmpty
    }
}

Describe 'Get-RestartFailureMessage' {
    It 'names the VM, the zone, the attempts and what is offline' {
        $message = Get-RestartFailureMessage -VmName 'rock-pr-test' -Zone 'us-east1-b' -Attempts 6 `
            -DownMessage 'Staging and the pr-* fleet are DOWN -- start it by hand.'

        $message | Should -BeLike '*rock-pr-test*'
        $message | Should -BeLike '*us-east1-b*'
        $message | Should -BeLike '*6 attempts*'
        $message | Should -BeLike '*pr-* fleet are DOWN*'
    }

    It 'refuses a blank down message rather than printing a generic one' {
        # A failed start on the fleet box takes staging and every pr-* site with
        # it. An operator reading a run that says only "failed to start" has not
        # been told what to go and start.
        { Get-RestartFailureMessage -VmName 'rock-pr-test' -Zone 'us-east1-b' -Attempts 6 -DownMessage '  ' } |
            Should -Throw '*naming what is offline*'
    }

    It 'does not double the spacing when the caller pads its sentence' {
        $message = Get-RestartFailureMessage -VmName 'vm' -Zone 'z' -Attempts 2 -DownMessage '  It is down.  '
        $message | Should -Be 'Failed to start VM vm in z after 2 attempts. It is down.'
    }
}
