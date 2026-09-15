# ADR-0009: The hand-deploy build stays a separate workflow from the artifact build

- **Status:** accepted
- **Date:** 2026-09-15
- **Governs:** `.github/workflows/ptp-14803-build-artifact.yml`, `.github/workflows/pr-test-artifact.yml`
- **Enforced by:** `Tests/PrTestEnvironments/test_pr_artifact_workflow.py`

## Context

`ptp-14803-build-artifact.yml` builds seven projects and stages four files -- two
assemblies and two Obsidian bundles -- for someone to copy onto a server by hand.
`pr-test-artifact.yml` builds the whole of RockWeb, packages it, and uploads the
zip to Cloud Storage. Fourteen of their step titles match, and nine of those
steps are byte-identical.

That shape is what an automated scan sees, and it has now produced the same
recommendation twice: delete the smaller file, or make it call the larger one
with inputs. The first review said "nobody triggers it" and "fifteen of nineteen
steps are duplicated." Both readings were answered in the test file at the time,
and the recommendation came back anyway, which is why it is written here instead.

## Decision

The two workflows stay separate files. The hand-deploy build is not retired, not
converted into a `workflow_call` of the artifact build, and its shared prelude is
not extracted into a composite action. What the two share is allowed to stay
duplicated, and a test pins the duplicated steps so they stay identical rather
than drifting apart unnoticed.

## Consequences

The property that makes the smaller file worth keeping is that it cannot reach
anything: `permissions: contents: read`, no secrets, no cloud session. The
artifact build resolves `PR_TEST_GCS_BUCKET` to the production file-storage
bucket and builds a sandbox connection string from the same credentials the
production deploy uses. Calling it from the hand-deploy path would put those
within reach of a build whose entire purpose is to be unable to touch them, and a
`workflow_call` cannot subtract a secret a called workflow already asks for.

The shared prelude is also not contiguous in either file. The artifact build
compiles Rock.JavaScript and Rock.JavaScript.EditorJs between steps the two have
in common, so one prelude action would have to build those for both callers or
split into three fragments. Neither is deeper than the nine duplicated steps, and
the duplication is held in place by a test instead.

The cost is real duplication: a fix to a shared step has to be made twice, and
the divergences that are deliberate -- the narrower node_modules cache, the
skipped `RockWeb\packages.config` restore -- have to be told apart from the ones
that are accidental by reading both files.

## What would reopen this

If the artifact build ever stops consuming secrets, or grows a secret-free build
job that the hand-deploy path could call, the isolation argument above no longer
requires two files and they should be merged. Equally, if the two preludes ever
become contiguous and identical in both, the composite action that was rejected
here becomes the cheaper option and is worth extracting.
