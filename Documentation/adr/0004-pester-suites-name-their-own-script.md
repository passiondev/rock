# ADR-0004: Pester suites name the script they lift from, at their own call site

- **Status:** accepted
- **Date:** 2026-09-15
- **Governs:** `Tests/PrTestEnvironments/Pester/*.Tests.ps1`, `Tests/PrTestEnvironments/Pester/ScriptFunctions.psm1`
- **Enforced by:** `Tests/PrTestEnvironments/test_powershell_job.py`, `Tests/PrTestEnvironments/test_ci_trigger_coverage.py`

## Context

Nine suites open the same way. Each resolves `Deploy-RockEnvironment.ps1`, then
dot-sources `Import-ScriptFunction` against it. Written out, suite after suite:

```powershell
$script:DeployScript = Get-RepositoryPath 'Deployment/PrTestEnvironments/Deploy-RockEnvironment.ps1'
. (Import-ScriptFunction -Path $script:DeployScript -Name 'Write-DeployStep', ...)
```

An architecture review read that as a prologue copied line for line and proposed
`New-DeployScriptFixture`, whose only input is the function list. The path would
move into the module, since a fixture that took it would not have shortened
anything.

This is the same shape ADR-0003 rejected for the Python suites, in PowerShell.
ADR-0003 records two earlier proposals of it. This was the third.

## Decision

A suite names the script it lifts from, in full, at the call site. No fixture,
helper or module function holds that path on a suite's behalf.

The ambient values a lifted body reads stay at the call site too. `-Supplied`
already names them, and that list is per-suite.

## Why

The proposal was built and measured rather than argued down.

It works. Converted, `DeploymentTarget.Tests.ps1` ran nineteen tests green, and
the prologue lost a line.

Then `test_every_suite_names_a_script_that_still_exists` went red, which is the
guard behaving correctly and is not the problem. The problem is the other check.

`test_ci_trigger_coverage.py` derives the CI `paths:` filter from the paths the
suites name. Converting all nine dropped this file's anchor count from nine to
one, and that suite stayed green the whole way down -- eight passed, at every
step. At zero, editing `Deploy-RockEnvironment.ps1` would stop triggering the
suites that test it, and nothing in the tree would say so.

That is ADR-0003's failure exactly: the check keeps passing while the coverage it
reports shrinks. Here it is worse, because the file losing coverage is the one
that deploys the site.

A second fact closes the remaining case for the fixture. A function exported from
`ScriptFunctions.psm1` cannot set `$script:` variables in a suite's scope -- that
name binds to the module. So the fixture could not absorb the ambient state
either; it could only hand back a scriptblock for the caller to dot-source, which
is what `Import-ScriptFunction` already is.

What was left to share is two lines, in four suites.

## Consequences

Nine suites carry a line they could have inherited, and moving or renaming a
deployment script is a nine-file edit.

`ScriptFunctions.psm1` takes a path and never resolves one. It has no opinion
about which script a suite lifts from, and that is deliberate.

A reviewer who has not traced the scan will read the repetition as an oversight,
because it looks exactly like one. That is what this record answers.

## What would reopen this

A scan that follows a fixture through to the path behind it, rather than matching
the literal call. ADR-0003 names the same opener for the Python half, and one
change would serve both.

Short of that: if `Get-RepositoryPath` is ever called somewhere the scan reads --
a manifest the suites and the workflow both consume -- the path stops being
duplicated without being hidden, and the fixture becomes free.
