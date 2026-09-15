<#
    The copy onto a live site runs without /MIR and without /PURGE. That is not an
    oversight -- a purge there would delete uploaded Content, the server's own
    fonts and 42 theme override files -- but it means nothing is ever removed from
    the site, so a file dropped between Rock versions stays in bin and in Blocks
    forever and the next version loads it.

    Both halves of that cost were paid on 2026-09-14, during the v19 production
    cutover:

      bin     v18's Rock.SignNow.dll survived the copy. It references
              Rock.Attribute.SystemEmailFieldAttribute, which v19 deleted outright
              with no [Obsolete] shim. MEF threw a TypeLoadException while building
              EntityTypeCache, Application_Start aborted, ASP.NET cached the
              failure for the life of the app domain, and every request to
              connect.passion.team returned 500. Moving one file fixed it.

      Blocks  92 v18 .ascx files survived. RegisterBlockTypes walks the filesystem
              and compiles each block to read its attributes, so all 92 were
              compiled at startup whether or not any page used them -- none were
              placed -- and they logged 82 DuplicateSystemGuidException in a single
              60-second burst.

    The two are treated differently on purpose, and these tests pin the difference.
    An orphaned core assembly breaks startup outright and its removal is
    unambiguous, so it is quarantined. An orphaned block file is noise, and Blocks
    is somewhere a plugin may legitimately have installed a file this pipeline
    knows nothing about, so it is only reported.

    The safety property that matters most here is the one in the middle: plugin
    assemblies are absent from the artifact by design -- they are installed onto
    the server, not shipped by the build -- so a reconciliation that did not
    distinguish them from orphans would delete every plugin on the site on every
    deploy. 'Rock.' with the dot is the whole of that distinction, and several
    tests below exist only to hold it still.
#>

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'ScriptFunctions.psm1') -Force

    $script:DeployScript = Get-RepositoryPath 'Deployment/PrTestEnvironments/Deploy-RockEnvironment.ps1'
    . (Import-ScriptFunction -Path $script:DeployScript -Name `
            'Write-DeployStep', 'Ensure-Directory', 'Invoke-OrphanedAssemblyReconciliation', `
            'Write-OrphanedBlockReport' `
        -Supplied 'DeployStartedUtc', 'DeployStepLogPath')

    # Write-DeployStep stamps against this and appends to the path below. Neither
    # is under test here; they just have to be set so the calls do not throw.
    $script:DeployStartedUtc = (Get-Date).ToUniversalTime()
    $script:DeployStepLogPath = ''

    function New-Assembly {
        param([string]$Directory, [string]$Name, [string]$Content = 'MZ-not-really-a-dll')
        Ensure-Directory -Path $Directory
        Set-Content -Path (Join-Path $Directory $Name) -Value $Content -NoNewline
    }

    # The gate in the function refuses to reconcile against an artifact carrying
    # fewer than 20 core assemblies, so a realistic artifact has to clear that bar
    # before any test of the interesting behaviour can run.
    function New-PlausibleArtifactBin {
        param([string]$Path, [int]$Count = 24)
        New-Assembly -Directory $Path -Name 'Rock.dll'
        for ($i = 1; $i -lt $Count; $i++) {
            New-Assembly -Directory $Path -Name ("Rock.Module{0}.dll" -f $i)
        }
    }
}

Describe 'Invoke-OrphanedAssemblyReconciliation' {

    BeforeEach {
        $script:SiteRoot = Join-Path $TestDrive ('site-' + [guid]::NewGuid().ToString('n'))
        $script:ArtifactRoot = Join-Path $TestDrive ('artifact-' + [guid]::NewGuid().ToString('n'))
        $script:Quarantine = Join-Path $TestDrive ('quarantine-' + [guid]::NewGuid().ToString('n'))

        $script:SiteBin = Join-Path $script:SiteRoot 'bin'
        $script:ArtifactBin = Join-Path $script:ArtifactRoot 'bin'

        New-PlausibleArtifactBin -Path $script:ArtifactBin

        # The site starts as a copy of the artifact, which is what robocopy leaves
        # behind. Every test then adds whatever the copy failed to remove.
        New-PlausibleArtifactBin -Path $script:SiteBin
    }

    It 'quarantines a core assembly the artifact no longer ships' {
        New-Assembly -Directory $script:SiteBin -Name 'Rock.SignNow.dll'

        $moved = @(Invoke-OrphanedAssemblyReconciliation -SiteRoot $script:SiteRoot `
                -ArtifactRoot $script:ArtifactRoot -QuarantineRoot $script:Quarantine)

        $moved | Should -Be @('Rock.SignNow.dll')
        Test-Path (Join-Path $script:SiteBin 'Rock.SignNow.dll') | Should -BeFalse
    }

    It 'moves it rather than deleting it' {
        # The backup taken earlier in the deploy also holds a copy, but an operator
        # reading the log at 2am should not have to go looking in a robocopy tree
        # to undo this.
        New-Assembly -Directory $script:SiteBin -Name 'Rock.SignNow.dll' -Content 'the-original-bytes'

        Invoke-OrphanedAssemblyReconciliation -SiteRoot $script:SiteRoot `
            -ArtifactRoot $script:ArtifactRoot -QuarantineRoot $script:Quarantine | Out-Null

        $quarantined = Join-Path $script:Quarantine 'Rock.SignNow.dll'
        Test-Path $quarantined | Should -BeTrue
        (Get-Content -Raw $quarantined) | Should -Be 'the-original-bytes'
    }

    It 'leaves plugin assemblies alone, whatever prefix they use' {
        # The artifact ships none of these. Treating "absent from the artifact" as
        # "orphaned" without the Rock. anchor would delete every plugin on the site
        # on every single deploy -- including the S3 storage provider that every
        # BinaryFileType on this catalog reads through.
        $plugins = @(
            'rocks.pillars.AmazonStorageProvider.dll',
            'rocks.pillars.ServiceReservation.dll',
            'com.passioncitychurch.Rock.dll',
            'com.bemaservices.OpenConnectionsDigest.dll',
            'org.ourcitychurch.ConnectionRequestDigest.dll'
        )
        foreach ($plugin in $plugins) { New-Assembly -Directory $script:SiteBin -Name $plugin }

        $moved = @(Invoke-OrphanedAssemblyReconciliation -SiteRoot $script:SiteRoot `
                -ArtifactRoot $script:ArtifactRoot -QuarantineRoot $script:Quarantine)

        $moved.Count | Should -Be 0
        foreach ($plugin in $plugins) {
            Test-Path (Join-Path $script:SiteBin $plugin) | Should -BeTrue -Because "$plugin belongs to the server, not to the build"
        }
    }

    It 'separates the orphan from the plugins in the same run' {
        # The shape of the actual outage: one fatal core orphan sitting in a bin
        # full of plugin assemblies that must not be touched.
        New-Assembly -Directory $script:SiteBin -Name 'Rock.SignNow.dll'
        New-Assembly -Directory $script:SiteBin -Name 'rocks.pillars.AmazonStorageProvider.dll'

        $moved = @(Invoke-OrphanedAssemblyReconciliation -SiteRoot $script:SiteRoot `
                -ArtifactRoot $script:ArtifactRoot -QuarantineRoot $script:Quarantine)

        $moved | Should -Be @('Rock.SignNow.dll')
        Test-Path (Join-Path $script:SiteBin 'rocks.pillars.AmazonStorageProvider.dll') | Should -BeTrue
    }

    It 'leaves core assemblies the artifact does ship' {
        $moved = @(Invoke-OrphanedAssemblyReconciliation -SiteRoot $script:SiteRoot `
                -ArtifactRoot $script:ArtifactRoot -QuarantineRoot $script:Quarantine)

        $moved.Count | Should -Be 0
        Test-Path (Join-Path $script:SiteBin 'Rock.dll') | Should -BeTrue
        Test-Path (Join-Path $script:SiteBin 'Rock.Module1.dll') | Should -BeTrue
    }

    It 'touches nothing when the artifact has no Rock.dll' {
        # The artifact is the authority for what should exist, so an artifact that
        # does not look like Rock must not be allowed to authorise removals. A
        # half-extracted zip would otherwise empty bin.
        Remove-Item (Join-Path $script:ArtifactBin 'Rock.dll') -Force
        New-Assembly -Directory $script:SiteBin -Name 'Rock.SignNow.dll'

        $moved = @(Invoke-OrphanedAssemblyReconciliation -SiteRoot $script:SiteRoot `
                -ArtifactRoot $script:ArtifactRoot -QuarantineRoot $script:Quarantine)

        $moved.Count | Should -Be 0
        Test-Path (Join-Path $script:SiteBin 'Rock.SignNow.dll') | Should -BeTrue
    }

    It 'touches nothing when the artifact ships too few core assemblies to be believable' {
        $sparse = Join-Path $TestDrive ('sparse-' + [guid]::NewGuid().ToString('n'))
        New-Assembly -Directory (Join-Path $sparse 'bin') -Name 'Rock.dll'
        New-Assembly -Directory $script:SiteBin -Name 'Rock.SignNow.dll'

        $moved = @(Invoke-OrphanedAssemblyReconciliation -SiteRoot $script:SiteRoot `
                -ArtifactRoot $sparse -QuarantineRoot $script:Quarantine)

        $moved.Count | Should -Be 0
        Test-Path (Join-Path $script:SiteBin 'Rock.SignNow.dll') | Should -BeTrue
    }

    It 'reports rather than acts when more assemblies are orphaned than a version bump explains' {
        # A version bump drops a handful. Dozens means the comparison is wrong, and
        # the right response to not understanding the input is to say so, not to
        # move files on a production site.
        for ($i = 0; $i -le 30; $i++) {
            New-Assembly -Directory $script:SiteBin -Name ("Rock.Stale{0}.dll" -f $i)
        }

        $moved = @(Invoke-OrphanedAssemblyReconciliation -SiteRoot $script:SiteRoot `
                -ArtifactRoot $script:ArtifactRoot -QuarantineRoot $script:Quarantine)

        $moved.Count | Should -Be 0
        Test-Path (Join-Path $script:SiteBin 'Rock.Stale0.dll') | Should -BeTrue
    }

    It 'does not create a quarantine directory when it has nothing to put in it' {
        Invoke-OrphanedAssemblyReconciliation -SiteRoot $script:SiteRoot `
            -ArtifactRoot $script:ArtifactRoot -QuarantineRoot $script:Quarantine | Out-Null

        Test-Path $script:Quarantine | Should -BeFalse
    }

    It 'skips quietly when the site has no bin at all' {
        $empty = Join-Path $TestDrive ('empty-' + [guid]::NewGuid().ToString('n'))
        Ensure-Directory -Path $empty

        { Invoke-OrphanedAssemblyReconciliation -SiteRoot $empty `
                -ArtifactRoot $script:ArtifactRoot -QuarantineRoot $script:Quarantine } |
            Should -Not -Throw
    }

    It 'ignores everything in bin that is not a dll' {
        New-Assembly -Directory $script:SiteBin -Name 'Rock.SignNow.pdb'
        New-Assembly -Directory $script:SiteBin -Name 'Rock.xml'

        $moved = @(Invoke-OrphanedAssemblyReconciliation -SiteRoot $script:SiteRoot `
                -ArtifactRoot $script:ArtifactRoot -QuarantineRoot $script:Quarantine)

        $moved.Count | Should -Be 0
        Test-Path (Join-Path $script:SiteBin 'Rock.xml') | Should -BeTrue
    }
}

Describe 'Write-OrphanedBlockReport' {

    BeforeAll {
        # The real check refuses to run against an artifact with fewer than 100
        # block files, on the same reasoning as the assembly gate: an artifact that
        # does not look like Rock cannot say what is orphaned. Rock ships 353.
        function New-PlausibleBlockTree {
            param([string]$Path, [int]$Count = 120)
            for ($i = 0; $i -lt $Count; $i++) {
                $sub = Join-Path $Path ("Domain{0}" -f ($i % 6))
                Ensure-Directory -Path $sub
                Set-Content -Path (Join-Path $sub ("Block{0}.ascx" -f $i)) -Value '<%@ Control %>' -NoNewline
            }
        }
    }

    BeforeEach {
        $script:SiteRoot = Join-Path $TestDrive ('bsite-' + [guid]::NewGuid().ToString('n'))
        $script:ArtifactRoot = Join-Path $TestDrive ('bart-' + [guid]::NewGuid().ToString('n'))
        $script:SiteBlocks = Join-Path $script:SiteRoot 'Blocks'
        $script:ArtifactBlocks = Join-Path $script:ArtifactRoot 'Blocks'

        New-PlausibleBlockTree -Path $script:ArtifactBlocks
        New-PlausibleBlockTree -Path $script:SiteBlocks
    }

    It 'names a block file the artifact no longer ships' {
        $orphan = Join-Path $script:SiteBlocks 'Domain0\Removed.ascx'
        Set-Content -Path $orphan -Value '<%@ Control %>' -NoNewline

        $found = @(Write-OrphanedBlockReport -SiteRoot $script:SiteRoot -ArtifactRoot $script:ArtifactRoot)

        $found | Should -Contain 'Domain0/Removed.ascx'.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    }

    It 'leaves the orphan on disk, because this one only reports' {
        # Unlike the assembly reconciliation. Blocks is somewhere a plugin may have
        # legitimately installed a file, and an orphan here is noisy rather than
        # fatal, so the decision stays with a person.
        $orphan = Join-Path $script:SiteBlocks 'Domain0\Removed.ascx'
        Set-Content -Path $orphan -Value '<%@ Control %>' -NoNewline

        Write-OrphanedBlockReport -SiteRoot $script:SiteRoot -ArtifactRoot $script:ArtifactRoot | Out-Null

        Test-Path $orphan | Should -BeTrue
    }

    It 'compares by path under Blocks, so the same name elsewhere is still an orphan' {
        # Block0.ascx exists in the artifact under Domain0. A copy under Domain5 is
        # a different block as far as RegisterBlockTypes is concerned, and it is
        # the duplicate GUID that produces the exception burst.
        $orphan = Join-Path $script:SiteBlocks 'Domain5\Block0.ascx'
        Set-Content -Path $orphan -Value '<%@ Control %>' -NoNewline

        $found = @(Write-OrphanedBlockReport -SiteRoot $script:SiteRoot -ArtifactRoot $script:ArtifactRoot)

        $found.Count | Should -Be 1
    }

    It 'finds nothing when the trees agree' {
        @(Write-OrphanedBlockReport -SiteRoot $script:SiteRoot -ArtifactRoot $script:ArtifactRoot).Count |
            Should -Be 0
    }

    It 'says nothing when the artifact has too few blocks to reconcile against' {
        $sparse = Join-Path $TestDrive ('bsparse-' + [guid]::NewGuid().ToString('n'))
        Ensure-Directory -Path (Join-Path $sparse 'Blocks')
        Set-Content -Path (Join-Path $sparse 'Blocks\Only.ascx') -Value '<%@ Control %>' -NoNewline

        @(Write-OrphanedBlockReport -SiteRoot $script:SiteRoot -ArtifactRoot $sparse).Count |
            Should -Be 0
    }

    It 'skips quietly when there is no Blocks directory' {
        $empty = Join-Path $TestDrive ('bempty-' + [guid]::NewGuid().ToString('n'))
        Ensure-Directory -Path $empty

        { Write-OrphanedBlockReport -SiteRoot $empty -ArtifactRoot $script:ArtifactRoot } | Should -Not -Throw
    }
}
