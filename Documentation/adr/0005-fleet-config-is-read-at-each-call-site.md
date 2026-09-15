# ADR-0005: The fleet config is read at each call site, and its values are pinned rather than shared

- **Status:** accepted
- **Date:** 2026-09-15
- **Governs:** `.github/pr-test-environments.json`, `.github/workflows/*.yml`
- **Enforced by:** `Tests/PrTestEnvironments/test_base_branch_config.py`, `Tests/PrTestEnvironments/test_gcp_session_consistency.py`

## Context

`.github/pr-test-environments.json` holds four values. Six workflows read it, in
three different ways. The values it holds appear again as literals: the bucket
expression seventeen times across twelve workflows, the trunk branch at eight
sites, the environment domain at six.

An architecture review counted that and proposed one reader step for the file,
with the copied literals collapsing behind it.

## Decision

Every call site reads the config itself and names the ref it reads from. Values
that GitHub Actions cannot take from a file stay written out, and a test pins
every copy to one spelling.

No workflow resolves the config on another's behalf.

## Why

Three separate things stop the adapter, and each is a property of GitHub Actions
rather than of this pipeline.

**The ref is the decision, not a detail.** `pr-test-deploy.yml` reads the config
from the default branch, so that flipping the trunk pin at cutover immediately
refuses the retired fleet. `pr-test-lifecycle.yml` reads it from `pull.base.ref`
on purpose, so `rock:destroy` keeps working on those same PRs after the flip.
Deploy fails closed; teardown stays open. One reader means one ref, and whichever
side it picks, the other breaks silently -- either the retired fleet goes on
deploying old-minor artifacts onto a migrated catalog, or it cannot be torn down
at all.

**A job's `env:` is evaluated before any step runs.** The bucket expression is
needed there, so it cannot come from a step output. A reader step is the wrong
shape for the value it would be asked to carry.

**A branch filter cannot be an expression.** `on: push: branches:` takes a
literal, so the trunk branch name has to be typed into each workflow that filters
on it. There is no value those sites could all read.

What is left for an adapter is a two-line base64 decode in three workflows. Behind
it would sit a fourth way of reading this file, in a place where the differences
between the existing three are load-bearing.

## Consequences

The counts stay high, and they read as an oversight to anyone who has not traced
why each copy is where it is.

The guards carry that weight instead. `BaseBranchCutoverPinTests` names every site
holding the trunk branch and fails on any that disagrees, which is what makes a
cutover a mechanical edit rather than a hand search -- it has missed
`production-deploy.yml` twice. `GcsBucketFallbackTests` holds all seventeen bucket
copies byte-identical and refuses a copy anywhere but a job-level `env:`, which is
what stops gratuitous ones accumulating beside the load-bearing ones.

## What would reopen this

Job-level `env:` accepting a step output, or `branches:` accepting an expression.
Either one removes the reason the copies exist, and the adapter becomes the
obvious shape rather than a lossy one.

Short of that, a reader that took the ref as a required input would preserve the
asymmetry -- but it would still only reach the two-line decode, so it is worth
building only once it can reach more than that.
