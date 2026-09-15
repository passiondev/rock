"""One guard for one fact: the shared catalog is a straight copy of production.

The claim that it is sanitized was written into six operational surfaces and
corrected in two of them on 2026-08-19. The correction was pinned by
`test_runbooks.py` and `test_status_comment_script.py`, but each of those reads
exactly one file, so the four uncorrected copies stayed green for two days --
including the pull request template, which every contributor reads, and the
operator runbook, which reasons from `sanitized` to "the exposure is bounded"
while describing a firewall that is open to the internet.

A per-file pin cannot catch that. This test names the surfaces instead, so the
next surface added is a line in one list rather than a file nobody thought of.
"""

import re
import textwrap
import unittest
from typing import NamedTuple

import pipeline_harness as harness


REPO_ROOT = harness.REPO_ROOT

# Everything an engineer or an operator reads to decide how to treat what they
# see in a PR environment. Requirement documents under `Discussion Docs` are
# deliberately absent: the PRD records what was asked for, carries its own
# status note saying the sanitization step does not exist, and is preserved as
# written rather than edited to match reality.
#
# Written out one quoted segment at a time, rather than as a list of
# slash-joined strings, because that literal form is the only one
# test_ci_trigger_coverage.py can see. Built from strings, these eight surfaces
# were invisible to it, so the CI trigger was never checked against them -- they
# were covered only because `.github/**` and `Documentation/**` happened to be
# wide enough.
CATALOG_SURFACES = [
    REPO_ROOT / "Documentation" / "PR-Test-Environments-Developer-Runbook.md",
    REPO_ROOT / "Documentation" / "PR-Test-Environments-Operator-Runbook.md",
    REPO_ROOT / "Documentation" / "Local-Engineering-Training-Edit-Test-and-Deploy.md",
    REPO_ROOT / "Documentation" / "Training" / "Facilitator-Script-Rock-CICD-Training.md",
    REPO_ROOT / "Documentation" / "Training" / "rock-cicd-training-deck.html",
    REPO_ROOT / "Documentation" / "Training" / "rock-cicd-cheat-sheet.html",
    REPO_ROOT / ".github" / "PULL_REQUEST_TEMPLATE.md",
    REPO_ROOT / ".github" / "scripts" / "pr-test-status.js",
    REPO_ROOT / "Documentation" / "Making-A-Change-To-Rock.md",
    # The domain model. It defines what "the shared catalog" means, so every
    # other surface here is describing whatever this file says it is.
    REPO_ROOT / "CONTEXT.md",
]

# Files that talk about PR environments and are still not catalog surfaces. Each
# is here for a reason that does not expire, and the sweep below fails on anything
# that is neither listed above nor excluded here -- so a new runbook is a red build
# rather than a file nobody thought of.
NOT_A_SURFACE = {
    # Requirement documents record what was asked for. The PRD carries its own
    # status note saying the sanitization step does not exist, and editing it to
    # match reality would destroy the evidence of what was assumed.
    "Documentation/Discussion Docs",
    # An incident report is a record of a moment. Correcting its wording forward
    # would make it describe a system that is not the one the incident happened to.
    "Documentation/Incidents",
    # The open-items log quotes the wrong wording in order to record that it was
    # corrected. A guard against the claim cannot run over the document that
    # explains the guard.
    "Documentation/Training/DevOps-Open-Items-Rock-CICD.md",
    # The production upgrade runbook is about production's own catalog. It matches
    # the sweep for two incidental reasons -- it points the reader at the PR
    # environment runbooks, and the scheduled task production installs is named
    # `Rock PR Environment Command Queue` because the installer is shared with the
    # test fleet. Neither makes it a catalog surface. The claims above are about the
    # sandbox copy on `connect-restore-test`; production's catalog carries the same
    # NAME, `RockConnectProd`, on a different instance, and checking this file for
    # promises about the sandbox's data would be checking the wrong database.
    "Documentation/Production-Upgrade-Runbook.md",
}

# What makes a file a candidate: it is prose an engineer or operator reads, and it
# mentions the thing. Extensions rather than a directory, because the surfaces are
# already split across Documentation/ and .github/.
CANDIDATE_SUFFIXES = {".md", ".html", ".js"}
CANDIDATE_MENTION = re.compile(r"shared catalog|PR environment|PR test environment", re.IGNORECASE)

class Claim(NamedTuple):
    """One sentence a catalog surface must not assert, and how to say so."""

    #: Searched against the whole document, so it carries its own boundaries.
    pattern: str
    #: Printed in the failure, phrased as what the document does wrong.
    why: str
    #: Whether a denial standing in front of a match makes it a correction rather
    #: than the claim. False for the two needles whose own wording is a negation:
    #: `is not real` carries a `not`, so a denial window would clear every
    #: occurrence and the needle would check nothing.
    denial_clears: bool = True


# A correction has to be able to name the thing it corrects, so a needle wide
# enough to catch the claim catches the sentence that replaced it too. These are
# how the surfaces word a denial, and they come in two directions.
#
# In front of the claim is the common one -- `the sandbox is **not** refreshed on
# a schedule`. Behind it is the shape the dated corrections use, where the
# sentence reports what the document used to say and then withdraws it: `said the
# database was sanitized until 2026-08-21`, `anything that assumes an overnight
# reset is wrong`. A backward-only window reads the first half of those as the
# claim, which is what this file did until the needles were widened far enough to
# reach them.
DENIAL_BEFORE = re.compile(r"not\b|never|no\b|despite|until 20\d\d")
DENIAL_AFTER = re.compile(r"until 20\d\d|(?:is|was|are|were) (?:wrong|false)|no longer|does not exist")
DENIAL_WINDOW = 120


def is_denied(text, match):
    """Whether `match` sits inside a sentence withdrawing the claim it names."""
    before = text[max(0, match.start() - DENIAL_WINDOW):match.start()]
    after = text[match.end():match.end() + DENIAL_WINDOW]
    return bool(DENIAL_BEFORE.search(before) or DENIAL_AFTER.search(after))

# The words a surface reaches for to say the data is safe to treat casually. What
# this replaced was the six literal strings that happened to be in the tree on
# 2026-08-19, so a writer who typed `scrubbed` where an earlier one typed
# `sanitized` went unseen. Held as words and combined below, so a synonym costs
# one line rather than six needles nobody thinks to add.
SAFETY_WORDS = (
    "sanitized", "sanitised",
    "scrubbed",
    "cleansed",
    "anonymized", "anonymised",
    "de-identified", "deidentified",
    "pseudonymized", "pseudonymised",
    "obfuscated",
)

# What those words attach to on these surfaces. `it`, `one` and `this` are here
# because that is how the sentences read -- `it's sanitized`, `not a sanitized
# one` -- and a noun list without them misses the shortest form of the claim.
CATALOG_NOUNS = (
    "sandboxe?s?", "catalogs?", "databases?", "data", "cop(?:y|ies)",
    "environments?", "it", "one", "this",
)

# How often a surface can promise the catalog comes back. The tree held `daily`
# and nothing else; `nightly` is the word the design documents used and the one a
# reader is most likely to write from memory.
SCHEDULE_WORDS = (
    "dail(?:y|ies)", "nightly", "overnight", "weekly", "hourly",
    "every (?:night|day|week|morning)", "each (?:night|day|week|morning)",
    "scheduled?",
)

# What it is the surface says happens on that schedule.
RESET_WORDS = ("refresh", "reset", "reseed", "re-seed", "restore", "rebuild", "wipe")


# What stands between the noun and the word. The apostrophe form carries no space
# in front of it -- the training deck said `It's sanitized` -- so it cannot be one
# more alternative in a list the `\s+` is factored out of.
COPULA = r"(?:\s+(?:is|are|was|were|has been|have been)|['\u2019]?s|['\u2019]re)\s+"

# Words allowed to stand between the two halves of a claim -- `the database is
# *fully* scrubbed`. A denial is not one of them: `it is not sanitized` is the
# correction, and a filler that swallows the `not` turns the fix into a failure
# no matter how far the denial window reaches.
FILLER = r"(?:(?!not\b|never\b|no\b)\w+\s+)"


def _nouns():
    return "|".join(CATALOG_NOUNS)


def _safety_claims():
    """A `Claim` for every way a surface can call the catalog safe.

    Two shapes, because the corrected history holds both: the word in front of
    the noun (`sanitized sandbox`) and the noun in front of the word (`the
    database is already scrubbed`)."""
    for word in SAFETY_WORDS:
        why = f"calls the catalog {word}"
        yield Claim(rf"(?i)\b{word}\s+(?:{_nouns()})\b", why)
        yield Claim(rf"(?i)\b(?:{_nouns()}){COPULA}{FILLER}{{0,2}}{word}\b", why)


def _refresh_claims():
    """A `Claim` for every way a surface can promise the catalog resets.

    Both orders again -- `nightly refresh` and `refreshed every night` -- and the
    reset word carries its own suffixes rather than being spelled out per tense."""
    for schedule in SCHEDULE_WORDS:
        for reset in RESET_WORDS:
            why = "promises a scheduled refresh that does not exist"
            yield Claim(rf"(?i)\b{schedule}[- ]{reset}(?:e?[ds])?\b", why)
            yield Claim(rf"(?i)\b{reset}(?:e?[ds])?\s+{FILLER}{{0,4}}?{schedule}\b", why)


# Claims that the data is safe to treat casually. The first two are written out
# because their own wording carries the negation the window looks for.
UNSAFE_CLAIMS = [
    Claim(r"is not real", "says the data is not real", denial_clears=False),
    Claim(
        r"cannot see production data",
        "says the environments cannot see production data",
        denial_clears=False,
    ),
    *_safety_claims(),
]

# Claims that the catalog resets. It has had no data load since 2026-04-14 and
# no scheduler exists, so anything promising a refresh tells the reader their
# test data will disappear when it will not, and that the drift self-corrects
# when it does not.
UNSAFE_REFRESH_CLAIMS = [
    Claim(r"wiped by a sandbox refresh", "promises a sandbox refresh that does not exist"),
    *_refresh_claims(),
]


class SharedCatalogClaimTests(harness.HarnessAssertions, unittest.TestCase):
    # The three surfaces where somebody decides whether to paste a screenshot into
    # a ticket, and the words each has to carry.
    #
    # Written out one quoted segment at a time for the same reason the module
    # header gives: joined from a string, these three paths are invisible to
    # test_ci_trigger_coverage.py and nothing checks the CI trigger against them.
    REQUIRED_CORRECTIONS = [
        (REPO_ROOT / "Documentation" / "PR-Test-Environments-Developer-Runbook.md", "not sanitized"),
        (REPO_ROOT / ".github" / "PULL_REQUEST_TEMPLATE.md", "real congregant data"),
        (REPO_ROOT / ".github" / "scripts" / "pr-test-status.js", "not a sanitized one"),
    ]

    def surface_texts(self):
        """(repository-relative name, contents) for every listed catalog surface.

        The name rather than the path, because it is what the failure messages
        print and an absolute path from somebody else's machine is noise."""
        for path in CATALOG_SURFACES:
            relative = path.relative_to(REPO_ROOT).as_posix()
            self.assertTrue(path.exists(), f"{relative} is listed as a catalog surface but does not exist")
            yield relative, path.read_text(encoding="utf-8")

    def test_every_document_that_mentions_the_catalog_is_accounted_for(self):
        """The list above is the thing this module is, and a list goes stale.

        Naming the surfaces is what let one guard replace six per-file pins, and it
        is also the weakness: a runbook added next month is not on it, so it is not
        checked, and nothing says so. This sweeps the two directories the surfaces
        live in and requires every candidate to be either listed or excluded on
        purpose. `Making-A-Change-To-Rock.md` is the one it found -- a live document
        engineers are pointed at, never checked, carrying no claim today and nothing
        stopping one from being added."""
        listed = {path.relative_to(REPO_ROOT).as_posix() for path in CATALOG_SURFACES}
        candidates, unaccounted = [], []

        # The repository root is swept too, and not for completeness. CONTEXT.md
        # sits there, it is where the term is defined, and it named a catalog that
        # had been off the instance for days while every document downstream of it
        # was checked. The definition was the only thing nothing read.
        searched = sorted(
            {
                *(REPO_ROOT / "Documentation").rglob("*"),
                *(REPO_ROOT / ".github").rglob("*"),
                *REPO_ROOT.glob("*.md"),
            }
        )

        for path in searched:
            if not path.is_file() or path.suffix.lower() not in CANDIDATE_SUFFIXES:
                continue
            relative = path.relative_to(REPO_ROOT).as_posix()
            if not CANDIDATE_MENTION.search(path.read_text(encoding="utf-8", errors="ignore")):
                continue
            candidates.append(relative)
            if relative in listed:
                continue
            if any(relative == skip or relative.startswith(skip + "/") for skip in NOT_A_SURFACE):
                continue
            unaccounted.append(relative)

        self.assertNotVacuous(
            candidates,
            "nothing under Documentation/, .github/ or the repository root mentions the catalog",
        )
        self.assertEqual(
            [],
            unaccounted,
            "these describe PR environments and are neither checked as a catalog "
            "surface nor excluded from being one -- add to CATALOG_SURFACES, or to "
            "NOT_A_SURFACE with the reason:\n  " + "\n  ".join(unaccounted),
        )

    def test_the_domain_model_names_where_a_catalog_name_comes_from(self):
        """CONTEXT.md defined the shared catalog as a literal name, and that name had
        been off the instance for days before anyone noticed.

        Nothing could notice. The name lives in `secrets.DB_NAME`, which is not in
        this repository and not readable from a test, so a literal written here is a
        copy of a value with no way back to the original. The runbooks had already
        been corrected and this had not, which is the shape the drift always takes:
        the document people edit when something changes is the operational one, and
        the definition is the one they read.

        So the rule is that the row names its source. `secrets.DB_NAME` and
        `vars.STAGING_DB_NAME` are checkable claims, in a way that a catalog name is
        not, and a reader who follows them lands on the current value rather than a
        remembered one.
        """
        rows = {
            "the shared catalog": "vars.PR_TEST_DB_NAME",
            "the staging catalog": "vars.STAGING_DB_NAME",
        }
        context = (REPO_ROOT / "CONTEXT.md").read_text(encoding="utf-8")

        for term, source in rows.items():
            with self.subTest(term=term):
                line = [
                    candidate for candidate in context.splitlines()
                    if candidate.startswith(f"| **{term}**")
                ]
                self.assertEqual(1, len(line), f"CONTEXT.md has no single row defining {term}")
                self.assertIn(
                    source,
                    line[0],
                    f"the {term} row defines itself with a name rather than pointing at "
                    f"{source}, so nothing can tell when the name stops being true",
                )

    def claims_asserted(self, claims):
        """Every place a surface asserts one of `claims`, already formatted.

        One sweep for both lists. It used to be two, and they had drifted: the
        refresh sweep allowed a correction to name what it was correcting and the
        sanitization sweep did not, so the two lists could not be worded the same
        way and neither could be widened without checking which rule it fell
        under. The allowance belongs to the claim now, not to the caller."""
        for relative, text in self.surface_texts():
            lines = text.splitlines()
            for claim in claims:
                for match in re.finditer(claim.pattern, text):
                    if claim.denial_clears and is_denied(text, match):
                        continue
                    line = harness.line_of(text, match.start())
                    line_text = lines[line - 1].strip()
                    yield f"{relative}:{line} {claim.why} ({line_text[:90]!r})"

    def test_no_surface_calls_the_catalog_sanitized(self):
        offenders = list(self.claims_asserted(UNSAFE_CLAIMS))
        self.assertFalse(offenders, "the catalog is a straight copy of production:\n  " + "\n  ".join(offenders))

    def test_no_surface_promises_a_refresh(self):
        offenders = list(self.claims_asserted(UNSAFE_REFRESH_CLAIMS))
        self.assertFalse(offenders, "the catalog has had no data load since 2026-04-14:\n  " + "\n  ".join(offenders))

    def test_the_sweep_still_catches_the_wording_it_was_written_for(self):
        """The needles are derived now, so nothing in the tree proves they match.

        Every string here was in a real surface before 2026-08-19. A green sweep
        over a corrected tree is the expected result and also what a broken regex
        produces, and these are the sentences that tell the two apart."""
        corrected = {
            "sanitized sandbox": UNSAFE_CLAIMS,
            "a shared, sanitized copy of production": UNSAFE_CLAIMS,
            "It's sanitized, so screenshots are fine": UNSAFE_CLAIMS,
            "environments come up with sanitized data": UNSAFE_CLAIMS,
            "the database is fully scrubbed": UNSAFE_CLAIMS,
            "a cleansed copy of the production catalog": UNSAFE_CLAIMS,
            "the data is de-identified before it lands": UNSAFE_CLAIMS,
            "daily-refreshed": UNSAFE_REFRESH_CLAIMS,
            "refreshed from production on a daily basis": UNSAFE_REFRESH_CLAIMS,
            "wiped by a sandbox refresh": UNSAFE_REFRESH_CLAIMS,
            "a nightly refresh restores it": UNSAFE_REFRESH_CLAIMS,
            "the catalog is reseeded every night": UNSAFE_REFRESH_CLAIMS,
        }
        for sentence, claims in corrected.items():
            with self.subTest(sentence=sentence):
                matched = [claim.why for claim in claims if re.search(claim.pattern, sentence)]
                self.assertTrue(
                    matched,
                    f"no needle matches {sentence!r}, which was in the tree and is the "
                    f"wording the guard exists to catch",
                )

    def test_a_correction_is_not_read_as_the_claim(self):
        """The other half: the sentences that replaced them have to stay green.

        A needle wide enough to catch the claim is wide enough to catch its
        correction, and a guard that fails on the fix is a guard somebody
        deletes."""
        corrections = [
            "The sandbox is **not** refreshed on a schedule.",
            "A straight copy of a production backup: not sanitized, and not refreshed on any schedule.",
            "This is real congregant data, not a sanitized one.",
            "Someone else's change, not a refresh -- the sandbox has no scheduled reset.",
            "there is no nightly refresh to give up",
            "This paragraph said the database was sanitized until 2026-08-21.",
            "- **It is not sanitized.** This section said it was until 2026-08-21.",
            "Anything that assumes an overnight reset is wrong.",
        ]
        for sentence in corrections:
            with self.subTest(sentence=sentence):
                for claims in (UNSAFE_CLAIMS, UNSAFE_REFRESH_CLAIMS):
                    for claim in claims:
                        if not claim.denial_clears:
                            continue
                        for match in re.finditer(claim.pattern, sentence):
                            self.assertTrue(
                                is_denied(sentence, match),
                                f"{sentence!r} is a correction, and the guard reads it as "
                                f"a claim that {claim.why} ({match.group(0)!r})",
                            )

    def test_the_surfaces_a_contributor_reads_first_carry_the_correction(self):
        """Absence of the false claim is not the same as presence of the true
        one. These three are where somebody decides whether to paste a
        screenshot into a ticket, so they have to say it outright."""
        for path, needle in self.REQUIRED_CORRECTIONS:
            relative = path.relative_to(REPO_ROOT).as_posix()
            # Whitespace-flattened, because the template wraps at 90 columns and
            # `real congregant data` straddles the wrap. The needle used to carry
            # the line break, which made this guard fail on a reflow that changed
            # no words -- a red build for a paragraph that still says the right
            # thing, and the kind of failure that gets the needle deleted rather
            # than fixed.
            text = " ".join(path.read_text(encoding="utf-8").split())
            self.assertIn(
                needle,
                text,
                f"{relative} does not tell the reader the catalog holds real congregant data",
            )

    def test_the_correction_survives_a_reflow(self):
        """The flattening above is what makes the needles wrap-proof, and it is one
        call somebody can drop while the suite stays green on today's tree.

        `real congregant data` straddled a line break for a month and the guard
        held only because nobody reflowed the template. This rewraps each
        paragraph the needles live in at four widths none of the files use and
        requires the same answer from each, so the needle is pinned to the words
        rather than to where the paragraph happens to break today."""
        for path, needle in self.REQUIRED_CORRECTIONS:
            relative = path.relative_to(REPO_ROOT).as_posix()
            paragraph = " ".join(path.read_text(encoding="utf-8").split())
            for width in (40, 72, 90, 120):
                with self.subTest(surface=relative, width=width):
                    # `break_on_hyphens` off because the words being checked sit
                    # beside file names and flags that a hyphen split would cut in
                    # half -- that would be the wrapper failing, not the guard.
                    rewrapped = textwrap.fill(
                        paragraph, width=width, break_on_hyphens=False, break_long_words=False
                    )
                    self.assertIn(
                        needle,
                        " ".join(rewrapped.split()),
                        f"{relative} loses {needle!r} when rewrapped at {width} columns, "
                        f"so the guard is pinned to the line breaks and not the words",
                    )


if __name__ == "__main__":
    unittest.main()
