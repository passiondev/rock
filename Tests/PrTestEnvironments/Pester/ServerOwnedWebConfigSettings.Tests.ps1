<#
    Merge-ServerOwnedWebConfigSettings decides what happens to production's
    web.config during the v19 cutover.

    The situation it exists for, measured on 2026-09-14. RockWeb/web.config is
    byte-identical to unaltered upstream, so the artifact carries Rock's stock
    distribution PasswordKey, DataEncryptionKey and machineKey. Production's were
    set by hand on the server years ago and match no branch, 18.4.1 included.
    InPlace robocopy does not exclude web.config, so without this function the
    cutover overwrites all three: every staff login fails, every encrypted
    attribute stops decrypting, and every session drops -- at once, on the site
    the whole organisation uses.

    The opposite mistake is just as easy. 19.3.4 changes 376 lines of web.config
    against 18.4.1, nearly all of them bindingRedirect bumps. Simply preserving
    the server's file would leave v19 unable to load its own assemblies. That is
    why this is a merge and not a $PreservedFiles entry, and it is why the
    binding-redirect Context below matters as much as the key ones.

    Two properties are worth more than the rest of this file. The artifact must
    win everything not explicitly named -- a merge that leaked server values into
    v19's assembly bindings would be worse than the bug it replaces. And it has to
    be idempotent, because a redeploy runs it against the output of the last one.

    The last Context runs against the real RockWeb/web.config. Fixtures prove the
    patterns handle the shape written down here; only the shipped file proves they
    handle the shape that ships.
#>

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'ScriptFunctions.psm1') -Force

    $script:DeployScript = Get-RepositoryPath 'Deployment/PrTestEnvironments/Deploy-RockEnvironment.ps1'
    . (Import-ScriptFunction -Path $script:DeployScript -Name 'Merge-ServerOwnedWebConfigSettings', 'Set-ProductionCompilationSettings')

    $script:Keys = @('PasswordKey', 'DataEncryptionKey', 'RunJobsInIISContext', 'OrgTimeZone')
    $script:Prefixes = @('OldPasswordKey')

    # Shaped like the real pair: same elements, different values, and the server
    # carrying two settings and a control registration the artifact has never had.
    $script:Artifact = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <add key="PasswordKey" value="ARTIFACT-PASSWORD-KEY" />
    <add key="DataEncryptionKey" value="ARTIFACT-ENCRYPTION-KEY" />
    <add key="RunJobsInIISContext" value="False" />
    <add key="OrgTimeZone" value="Local" />
  </appSettings>
  <system.web>
    <machineKey validationKey="ARTIFACTVALIDATION" decryptionKey="ARTIFACTDECRYPTION" validation="SHA1" decryption="AES" />
    <compilation debug="true" targetFramework="4.7.2" />
    <httpRuntime maxRequestLength="102400" targetFramework="4.7.2" />
    <pages>
      <controls>
        <add tagPrefix="Rock" namespace="Rock.Web.UI.Controls" assembly="Rock" />
      </controls>
    </pages>
  </system.web>
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" />
        <bindingRedirect oldVersion="0.0.0.0-6.0.0.0" newVersion="6.0.0.0" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>
'@

    $script:Server = @'
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <appSettings>
    <add key="PasswordKey" value="SERVER-PASSWORD-KEY" />
    <add key="DataEncryptionKey" value="SERVER-ENCRYPTION-KEY" />
    <add key="OldPasswordKey" value="SERVER-OLD-KEY-0" />
    <add key="OldPasswordKey1" value="SERVER-OLD-KEY-1" />
    <add key="RunJobsInIISContext" value="True" />
    <add key="OrgTimeZone" value="Eastern Standard Time" />
    <add key="EnableBundling" value="false" />
    <add key="RedisConnectionString" value="localhost" />
  </appSettings>
  <system.web>
    <machineKey validationKey="SERVERVALIDATION" decryptionKey="SERVERDECRYPTION" validation="SHA1" decryption="AES" />
    <compilation debug="false" targetFramework="4.7.2" />
    <httpRuntime maxRequestLength="102400" targetFramework="4.7.2" />
    <pages>
      <controls>
        <add tagPrefix="Rock" namespace="Rock.Web.UI.Controls" assembly="Rock" />
        <add tagPrefix="Passion" assembly="com.passioncitychurch" namespace="com.passioncitychurch" />
      </controls>
    </pages>
  </system.web>
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" />
        <bindingRedirect oldVersion="0.0.0.0-5.2.3.0" newVersion="5.2.3.0" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>
'@

    $script:Merge = {
        param($Incoming, $Existing)
        Merge-ServerOwnedWebConfigSettings `
            -IncomingWebConfig $Incoming `
            -ExistingWebConfig $Existing `
            -AppSettingKeys $script:Keys `
            -AppSettingKeyPrefixes $script:Prefixes
    }
}

Describe 'Merge-ServerOwnedWebConfigSettings' {

    Context 'the production cutover: artifact over a server that owns its keys' {
        BeforeAll {
            $script:Result = & $script:Merge $script:Artifact $script:Server
            $script:Merged = $script:Result.WebConfig
        }

        It 'keeps the server PasswordKey, so existing logins still validate' {
            $script:Merged | Should -Match '<add key="PasswordKey" value="SERVER-PASSWORD-KEY" />'
            $script:Merged | Should -Not -Match 'ARTIFACT-PASSWORD-KEY'
        }

        It 'keeps the server DataEncryptionKey, so encrypted attributes still decrypt' {
            $script:Merged | Should -Match '<add key="DataEncryptionKey" value="SERVER-ENCRYPTION-KEY" />'
            $script:Merged | Should -Not -Match 'ARTIFACT-ENCRYPTION-KEY'
        }

        It 'keeps RunJobsInIISContext True, so scheduled jobs keep running' {
            $script:Merged | Should -Match '<add key="RunJobsInIISContext" value="True" />'
        }

        It 'keeps the server OrgTimeZone' {
            $script:Merged | Should -Match '<add key="OrgTimeZone" value="Eastern Standard Time" />'
        }

        It 'keeps the whole server machineKey element, so sessions survive' {
            $script:Merged | Should -Match 'validationKey="SERVERVALIDATION"'
            $script:Merged | Should -Match 'decryptionKey="SERVERDECRYPTION"'
            $script:Merged | Should -Not -Match 'ARTIFACTVALIDATION'
        }

        It 'carries the OldPasswordKey family, which cannot be listed in advance' {
            $script:Merged | Should -Match '<add key="OldPasswordKey" value="SERVER-OLD-KEY-0" />'
            $script:Merged | Should -Match '<add key="OldPasswordKey1" value="SERVER-OLD-KEY-1" />'
        }

        It 'carries a control registration the artifact does not have' {
            $script:Merged | Should -Match '<add tagPrefix="Passion" assembly="com.passioncitychurch"'
        }

        It 'reports everything it carried' {
            $script:Result.Carried | Should -Contain 'PasswordKey'
            $script:Result.Carried | Should -Contain 'DataEncryptionKey'
            $script:Result.Carried | Should -Contain 'RunJobsInIISContext'
            $script:Result.Carried | Should -Contain 'OrgTimeZone'
            $script:Result.Carried | Should -Contain 'machineKey'
            $script:Result.Carried | Should -Contain 'OldPasswordKey1'
            $script:Result.Carried | Should -Contain 'tagPrefix:Passion'
        }

        It 'reports the server settings it deliberately did not carry' {
            $script:Result.Dropped | Should -Contain 'EnableBundling'
            $script:Result.Dropped | Should -Contain 'RedisConnectionString'
        }

        It 'does not carry the settings it only reported' {
            $script:Merged | Should -Not -Match 'EnableBundling'
            $script:Merged | Should -Not -Match 'RedisConnectionString'
        }
    }

    Context 'everything the artifact owns still wins' {
        BeforeAll {
            $script:Result = & $script:Merge $script:Artifact $script:Server
            $script:Merged = $script:Result.WebConfig
        }

        # The reason this is a merge and not a $PreservedFiles entry. 19.3.4 bumps
        # these against 18.4.1 and its assemblies do not load without them.
        It 'keeps the artifact bindingRedirect and not the server one' {
            $script:Merged | Should -Match 'newVersion="6.0.0.0"'
            $script:Merged | Should -Not -Match 'newVersion="5.2.3.0"'
        }

        It 'leaves a control registration both files have alone' {
            ([regex]::Matches($script:Merged, '<add tagPrefix="Rock"')).Count | Should -Be 1
        }

        It 'does not duplicate any appSettings key' {
            foreach ($key in $script:Keys) {
                ([regex]::Matches($script:Merged, '<add key="' + $key + '"')).Count | Should -Be 1
            }
        }
    }

    Context 'a value containing a regex substitution token' {
        # Carried values are base64 and hex today. The day one is not is not the
        # day to discover that $1 in a replacement means something.
        BeforeAll {
            # .NET String.Replace and not PowerShell's -replace: the operator
            # expands $0 and $$ in its replacement, which would quietly rewrite
            # the fixture into something that no longer tests anything.
            $script:Odd = $script:Server.Replace('SERVER-PASSWORD-KEY', 'a$1b$0c$$d')
            $script:Result = & $script:Merge $script:Artifact $script:Odd
        }

        It 'carries the value literally' {
            $script:Result.WebConfig | Should -Match ([regex]::Escape('<add key="PasswordKey" value="a$1b$0c$$d" />'))
        }
    }

    Context 'a server with no previous web.config' {
        It 'returns the artifact unchanged' {
            $result = & $script:Merge $script:Artifact ''
            $result.WebConfig | Should -BeExactly $script:Artifact
            $result.Carried | Should -BeNullOrEmpty
        }
    }

    Context 'run twice, as a redeploy does' {
        It 'is idempotent' {
            $once = (& $script:Merge $script:Artifact $script:Server).WebConfig
            $twice = (& $script:Merge $once $script:Server).WebConfig
            $twice | Should -BeExactly $once
        }
    }

    Context 'an artifact that has no entry for a key the server does' {
        BeforeAll {
            $stripped = $script:Artifact -replace '\s*<add key="RunJobsInIISContext" value="False" />', ''
            $script:Result = & $script:Merge $stripped $script:Server
        }

        It 'adds the server entry rather than losing the setting' {
            $script:Result.WebConfig | Should -Match '<add key="RunJobsInIISContext" value="True" />'
            $script:Result.Carried | Should -Contain 'RunJobsInIISContext'
        }

        It 'adds it exactly once' {
            ([regex]::Matches($script:Result.WebConfig, '<add key="RunJobsInIISContext"')).Count | Should -Be 1
        }
    }

    Context 'composed with Set-ProductionCompilationSettings, as the deploy composes them' {
        It 'carries the keys and still turns debug off' {
            $merged = (& $script:Merge $script:Artifact $script:Server).WebConfig
            $final = Set-ProductionCompilationSettings -WebConfig $merged
            $final | Should -Match '<add key="PasswordKey" value="SERVER-PASSWORD-KEY" />'
            $final | Should -Match '<compilation[^>]*debug="false"'
            $final | Should -Match '<httpRuntime[^>]*executionTimeout="600"'
        }
    }

    Context 'the real RockWeb/web.config, which is what actually ships' {
        BeforeAll {
            # Anchored on the directory, the way CompilationSettings.Tests.ps1
            # reaches the same file. Get-RepositoryPath resolves either, but the
            # suite sweep in test_powershell_job.py requires a directory for
            # anything that is not a .ps1.
            $script:Shipped = Get-Content -Raw -Path (Join-Path (Get-RepositoryPath 'RockWeb') 'web.config')
        }

        # If any of these stop matching, the merge silently carries nothing and
        # the cutover overwrites production's keys exactly as it would have
        # without this function. That is the failure this Context exists to catch.
        It 'has an appSettings entry the patterns find for <_>' -ForEach @(
            'PasswordKey', 'DataEncryptionKey', 'RunJobsInIISContext', 'OrgTimeZone'
        ) {
            $script:Shipped | Should -Match ('<add\s+key="' + $_ + '"[^>]*>')
        }

        It 'has a machineKey element the pattern finds' {
            $script:Shipped | Should -Match '<machineKey\b[^>]*>'
        }

        It 'has a controls section to carry registrations into' {
            $script:Shipped | Should -Match '<controls[^>]*>'
        }

        It 'merges a server file onto it without losing the artifact assembly bindings' {
            $result = & $script:Merge $script:Shipped $script:Server
            $result.WebConfig | Should -Match '<add key="PasswordKey" value="SERVER-PASSWORD-KEY" />'
            $result.WebConfig | Should -Match '<add tagPrefix="Passion"'
            # Whatever the shipped file redirects to, it is still there afterwards.
            $before = ([regex]::Matches($script:Shipped, '<bindingRedirect ')).Count
            ([regex]::Matches($result.WebConfig, '<bindingRedirect ')).Count | Should -Be $before
        }
    }
}
