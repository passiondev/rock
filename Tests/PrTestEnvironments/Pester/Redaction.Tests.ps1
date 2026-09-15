<#
    The command queue writes the command's own output to a log that is uploaded
    beside the result and printed into a GitHub Actions run. This repository is
    public, and the command carries a SQL connection string with a plaintext
    password in it.

    The Python suite asserts that the string "<redacted>" appears somewhere in
    the script. That assertion cannot fail while the word survives in a comment,
    and it has never once established that a password is actually removed.
#>

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'ScriptFunctions.psm1') -Force

    $script:QueueScript = Get-RepositoryPath 'Deployment/PrTestEnvironments/Invoke-PrEnvironmentCommandQueue.ps1'
    . (Import-ScriptFunction -Path $script:QueueScript -Name 'Get-RedactedText', 'Get-CommandSecrets', 'Get-SecretFieldNamePattern', 'Get-PasswordValuePattern')

    $script:Password = 'hunter2-correct-horse'
    $script:ConnectionString = "Data Source=tcp:10.0.0.5,1433;Initial Catalog=RockSandbox;User Id=rock;password=$($script:Password);Encrypt=true;"
}

Describe 'Get-RedactedText' {
    It 'removes a secret it was handed' {
        $result = Get-RedactedText -Text "connecting with $($script:ConnectionString)" -Secrets @($script:ConnectionString)

        $result | Should -Not -Match ([regex]::Escape($script:Password))
        $result | Should -Match '<redacted>'
    }

    It 'removes a password even when it was handed no secrets at all' {
        # The backstop, and the one that matters. Anything the queue failed to
        # collect still has to be caught on the way out, because by the time this
        # text is wrong it is already in a public log.
        $result = Get-RedactedText -Text $script:ConnectionString -Secrets @()

        $result | Should -Not -Match ([regex]::Escape($script:Password))
        $result | Should -Match 'password=<redacted>'
    }

    It 'matches the keyword whatever its casing' {
        foreach ($spelling in 'password', 'Password', 'PASSWORD') {
            $result = Get-RedactedText -Text "Server=x;$spelling=$($script:Password);" -Secrets @()
            $result | Should -Not -Match ([regex]::Escape($script:Password))
        }
    }

    It 'stops at the delimiter so the rest of the connection string still reads' {
        # A log that redacts the whole line cannot answer the question these logs
        # exist to answer, which is usually "which catalog did it land on".
        $result = Get-RedactedText -Text $script:ConnectionString -Secrets @()

        $result | Should -Match 'Initial Catalog=RockSandbox'
        $result | Should -Match 'Encrypt=true'
    }

    It 'leaves a short secret alone, by design' {
        # Redacting a three-character string would riddle the log with <redacted>
        # and hide the detail the log was opened for. Pinned so the threshold is a
        # decision rather than an accident.
        $result = Get-RedactedText -Text 'the id is abc and that is fine' -Secrets @('abc')

        $result | Should -Be 'the id is abc and that is fine'
    }

    It 'returns an empty string for no input rather than throwing' {
        Get-RedactedText -Text '' -Secrets @($script:Password) | Should -Be ''
        Get-RedactedText -Text $null -Secrets @($script:Password) | Should -Be ''
    }

    It 'removes every occurrence, not just the first' {
        $text = "$($script:ConnectionString) ... retrying ... $($script:ConnectionString)"
        $result = Get-RedactedText -Text $text -Secrets @($script:ConnectionString)

        $result | Should -Not -Match ([regex]::Escape($script:Password))
    }
}

Describe 'Get-CommandSecrets' {
    It 'collects both connection string properties a command can carry' {
        $command = [pscustomobject]@{
            connectionString        = 'Server=a;password=first-password-here;'
            sandboxConnectionString = 'Server=b;password=second-password-here;'
        }

        $secrets = Get-CommandSecrets -Command $command

        $secrets | Should -Contain 'Server=a;password=first-password-here;'
        $secrets | Should -Contain 'Server=b;password=second-password-here;'
    }

    It 'emits the bare password as well as the whole string' {
        # The full string only matches if the log reproduced it intact. Output that
        # wrapped or partly quoted it would slip a whole password through.
        $command = [pscustomobject]@{ connectionString = $script:ConnectionString }

        Get-CommandSecrets -Command $command | Should -Contain $script:Password
    }

    It 'returns nothing for a command that carries no connection string' {
        $command = [pscustomobject]@{ commandId = 'deploy-1'; command = 'deploy' }

        @(Get-CommandSecrets -Command $command).Count | Should -Be 0
    }

    It 'ignores a property that is present but blank' {
        $command = [pscustomobject]@{ connectionString = '   ' }

        @(Get-CommandSecrets -Command $command).Count | Should -Be 0
    }

    It 'collects a secret-shaped field name it has never been told about' {
        # The same five names QueueCommand.Tests.ps1 puts through the producer.
        # This side used to hold a literal list of two, so every one of these was
        # redacted in the public Actions log and printed in full in the log the
        # agent uploads to the bucket -- the more durable of the two, and the one
        # a person actually goes and reads after a failure.
        foreach ($name in 'restoreConnectionString', 'adminPassword', 'apiToken', 'clientSecret', 'gcpCredential') {
            $command = [pscustomobject]@{ $name = "Server=a;password=$($script:Password);" }

            $secrets = Get-CommandSecrets -Command $command

            $secrets | Should -Contain "Server=a;password=$($script:Password);" -Because "$name reads as a secret"
            $secrets | Should -Contain $script:Password -Because "$name reads as a secret"
        }
    }

    It 'keys on the shape of the name whatever its casing' {
        $command = [pscustomobject]@{ SANDBOXCONNECTIONSTRING = "Server=a;password=$($script:Password);" }

        Get-CommandSecrets -Command $command | Should -Contain $script:Password
    }

    It 'leaves a field that does not read as a secret to the keyword backstop' {
        # Get-CommandSecrets is the name half only. A password inside a field
        # called `notes` is Get-RedactedText's job, and the pair below proves it.
        $command = [pscustomobject]@{ notes = "connecting with password=$($script:Password);" }

        @(Get-CommandSecrets -Command $command).Count | Should -Be 0
    }
}

Describe 'the two together' {
    It 'keeps a password out of a log built from a real command' {
        # Neither function is correct alone: one decides what counts as a secret and
        # the other applies it. This is the pair as the queue actually calls it.
        $command = [pscustomobject]@{
            commandId               = 'deploy-14-981'
            command                 = 'deploy'
            sandboxConnectionString = $script:ConnectionString
        }
        $log = @(
            'Starting deploy for PR 14',
            "Using $($script:ConnectionString)",
            "Password was $($script:Password)",
            'Deploy finished'
        ) -join "`n"

        $result = Get-RedactedText -Text $log -Secrets (Get-CommandSecrets -Command $command)

        $result | Should -Not -Match ([regex]::Escape($script:Password))
        $result | Should -Match 'Starting deploy for PR 14'
        $result | Should -Match 'Deploy finished'
    }
}
