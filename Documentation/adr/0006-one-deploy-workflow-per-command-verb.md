# ADR-0006: The deploy path is one workflow per command verb, not one deploy workflow

- **Status:** accepted
- **Date:** 2026-09-15
- **Governs:** `.github/workflows/env-deploy-command.yml`, `.github/workflows/pr-test-deploy.yml`, `.github/actions/report-script-drift/`
- **Enforced by:** `Tests/PrTestEnvironments/test_environment_deploy.py`, `.github/actions/queue-vm-command/Write-VmCommand.ps1`

## Context

Four workflows deploy. Staging and production both call `env-deploy-command.yml`,
which queues a `deploy-environment` command and waits. The pr-* fleet queues a
`deploy` command from `pr-test-deploy.yml` and waits for itself.

Read from the workflow directory this looks like three callers where there should
be four, and an architecture review proposed exactly that: route the PR deploy
through `env-deploy-command.yml` and leave the fleet-only steps around it. A
reviewer will propose it again, because from that directory it is the obvious
shape.

The review was right about something else in the same breath, and that half was
built -- see the Consequences.

## Decision

The fleet keeps its own deploy workflow, and `env-deploy-command.yml` stays the
caller for `deploy-environment` alone. What the two paths share is shared through
composite actions, one behaviour at a time, and never by making one workflow serve
both verbs.

Merging all four into a single deploy workflow is rejected outright.

## Why

The verb is not a parameter. It is the name of a contract, and the queue agent on
the VM dispatches on it to a different script:

| verb | script | lines | VM timeout |
|---|---|---|---|
| `deploy` | `Deploy-PrEnvironment.ps1` | 369 | 1500s |
| `deploy-environment` | `Deploy-RockEnvironment.ps1` | 2897 | 1800s |

They are not two dialects of one deploy. The first creates an IIS site per PR
under `C:\RockTestEnvs`, binds a wildcard certificate to it and backfills the
shared assets no artifact carries. The second deploys a long-lived named
environment, in `DedicatedSite` or `InPlace`, with a backup, a step log and a
rollback copy. `Invoke-PrEnvironmentCommandQueue.ps1` holds both contracts, and
they do not overlap: `deploy` requires `prNumber`, `deploy-environment` requires
`environmentName`, and neither accepts the other's.

The secret field is the part that cannot be reconciled at all. `deploy` reads
`sandboxConnectionString` and `deploy-environment` reads `connectionString` --
two live names for one concept, recorded in `Write-VmCommand.ps1`. One workflow
sending one field name means renaming the other on the VM, and the queue agent
only reaches a VM through a `workflow_dispatch` bootstrap. So the rename has to
land on every box before the workflow that needs it, and until it does, every
deploy of that verb fails. There is no ordering of that change that is safe on the
morning somebody needs to deploy.

A merged workflow would also have to carry production's `InPlace` mode, backup
root and approval gate past the fleet on every run, and the fleet's per-PR site
creation past production. Each is optional to the other and fatal if it leaks.

## Consequences

The deploy workflows stay at four files and read as more duplication than they
are. Anyone counting entry points will find the same thing the review found.

What was genuinely shared has moved. The deploy script drift comparison was sixty
lines inlined in `env-deploy-command.yml`, so only staging and production ran it
-- while the fleet, whose `Deploy-PrEnvironment.ps1` sits in the very same
published set, never did. It is now `.github/actions/report-script-drift`, both
paths run it, and its branches are executed by Pester rather than parsed. The
artifact existence check went to the fleet as six copied lines, deliberately:
behind an action its interface would be as large as its implementation.

That is the shape further sharing takes. One behaviour, one action, both callers.

## What would reopen this

The queue agent growing one deploy verb that both paths can use, with one required
field set and one secret field name -- published to every VM first. At that point
the workflows are two callers of one contract and this decision is about nothing.

A second thing would do it: if `Deploy-PrEnvironment.ps1` and
`Deploy-RockEnvironment.ps1` converge to where the 369-line script is a thin call
into the 2897-line one, the verbs stop naming different work.
