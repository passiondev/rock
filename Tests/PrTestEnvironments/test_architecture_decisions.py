"""Keep the recorded decisions and the code that depends on them pointing at each other.

Card 06 of the 2026-08-26 architecture review: three times a review has proposed a
change this pipeline had already rejected for a reason that still holds. Twice the
reason was caught, both times because the reviewer happened to open a test whose
docstring carried it. The third got as far as a written card and a started
implementation before the reason surfaced -- that was card 04 of the same review,
withdrawn mid-flight once the scan it would have blinded was traced.

A docstring is the wrong home for that reasoning. Whoever edits the test reads it.
Whoever proposes changing the design does not open the test.

So the reasons moved to `Documentation/adr/`, and this file is what stops the move
from decaying into a directory nobody reads. It holds two directions in step:

- a citation the tree makes must name a record that exists, and
- a record that exists must be cited from the code it governs.

The second is the one that matters. An ADR nothing points at is invisible exactly
when it is needed, which is the failure this whole card is about, moved one level up.
"""

import pathlib
import re
import unittest

import pipeline_harness as harness

REPO_ROOT = harness.REPO_ROOT
ADR_DIR = REPO_ROOT / "Documentation" / "adr"
ADR_README = REPO_ROOT / "Documentation" / "adr" / "README.md"

# `ADR-0001`, the form the prose uses, and the form a citation must take. Matched
# case-sensitively: a lowercase `adr-0001` is a typo, and quietly accepting it means
# the citation check passes on a string no reader will follow.
CITATION = re.compile(r"ADR-(\d{4})")

# A record is a decision, not a rule, and the difference is whether it says what
# would change its mind. Anything without the last heading is a rule in ADR clothing.
REQUIRED_HEADINGS = (
    "## Context",
    "## Decision",
    "## Consequences",
    "## What would reopen this",
)

# The header fields every record carries. `Enforced by` is load-bearing rather than
# decorative: it is what the second direction of the citation check reads.
REQUIRED_FIELDS = (
    "- **Status:**",
    "- **Date:**",
    "- **Governs:**",
    "- **Enforced by:**",
)

# A heading with nothing under it passes a check that looks for the heading, and
# tells the reader nothing. The shortest real section is forty words -- `What
# would reopen this` in ADR-0003 -- so this sits well under every record written
# so far and well over a stub, which is the only line it has to draw.
MINIMUM_SECTION_WORDS = 20

# What a field holds when somebody meant to come back to it. A `Status:` reading
# `TBD` is worse than a missing one: the shape check goes green and the reader
# takes it as a decision.
PLACEHOLDER = re.compile(r"(?i)^(?:tbd|todo|t\.?b\.?d\.?|n/?a|xxx|\?+|<[^>]*>)\W*$")


def adr_files():
    """Every record in the directory, README excluded, ordered by number."""
    return sorted(path for path in ADR_DIR.glob("*.md") if path.name != "README.md")


def sections_of(text):
    """`{heading: body}` for every `##` section of a record.

    Split on the heading line rather than searched for it, so a body is everything
    up to the next heading. That is what `carries text` has to measure: a heading
    followed straight by the next heading yields an empty body, and an empty body
    is the case this reader exists to make visible."""
    parts = re.split(r"^(## .+)$", text, flags=re.MULTILINE)
    return {parts[i].strip(): parts[i + 1] for i in range(1, len(parts), 2)}


def field_value(text, field):
    """What a record writes after one of its header fields, or None if absent.

    The first matching line only. ADR-0002 carries `- **` bullets inside its
    sections as well, and a sweep of every bold bullet would read those as header
    fields."""
    for line in text.splitlines():
        if line.startswith(field):
            return line[len(field):].strip()
    return None


def adr_number(path):
    """The four-digit number a record's filename starts with."""
    return path.name.split("-", 1)[0]


def cited_files_of(path):
    """Repository-relative paths a record names in its `Enforced by:` field."""
    for line in path.read_text().splitlines():
        if not line.startswith("- **Enforced by:**"):
            continue
        return re.findall(r"`([^`]+)`", line)
    return []


class RecordShapeTests(harness.HarnessAssertions, unittest.TestCase):
    """Each record carries the sections that make it a decision rather than a rule,
    and fills them. The headings were checked for presence alone until 2026-09-15,
    which meant a record could satisfy every one of them and say nothing."""

    def test_there_are_records_to_check(self):
        self.assertNotVacuous(
            adr_files(),
            f"{ADR_DIR} holds no records, so every other test in this file passes "
            "by finding nothing.",
        )

    def test_every_record_says_what_would_reopen_it(self):
        for path in adr_files():
            with self.subTest(record=path.name):
                text = path.read_text()
                for heading in REQUIRED_HEADINGS:
                    self.assertIn(
                        heading,
                        text,
                        f"{path.name} has no `{heading}` section. A record that does "
                        "not say what would change its mind is a rule, and a rule "
                        "with no stated cost gets followed until it is wrong.",
                    )

    def test_every_record_names_what_enforces_it(self):
        for path in adr_files():
            with self.subTest(record=path.name):
                text = path.read_text()
                for field in REQUIRED_FIELDS:
                    self.assertIn(
                        field,
                        text,
                        f"{path.name} is missing its `{field}` header field.",
                    )

    def test_every_required_section_carries_text(self):
        """The headings are checked for presence above, which a skeleton satisfies.

        Whoever writes the next record copies the last one, and a copied record
        arrives with the right headings and the previous record's reasoning deleted
        from under them. `What would reopen this` is the one that matters and the
        one most likely to be left blank -- it is the section a writer reaches
        last, after the decision already feels settled."""
        for path in adr_files():
            sections = sections_of(path.read_text())
            for heading in REQUIRED_HEADINGS:
                with self.subTest(record=path.name, section=heading):
                    body = sections.get(heading, "")
                    self.assertGreaterEqual(
                        len(body.split()),
                        MINIMUM_SECTION_WORDS,
                        f"{path.name} has the `{heading}` heading and "
                        f"{len(body.split())} words under it. A heading with nothing "
                        "beneath it reads as an answered question, so the reader stops "
                        "there rather than asking.",
                    )

    def test_every_header_field_carries_a_value(self):
        """Same gap one level up. `- **Enforced by:**` with nothing after it satisfies
        the field check and then reads back as an empty enforcer list in
        `cited_files_of`, which is the field the citation direction runs on."""
        for path in adr_files():
            text = path.read_text()
            for field in REQUIRED_FIELDS:
                with self.subTest(record=path.name, field=field):
                    value = field_value(text, field)
                    self.assertTrue(
                        value,
                        f"{path.name} writes `{field}` and leaves it empty.",
                    )
                    self.assertIsNone(
                        PLACEHOLDER.match(value),
                        f"{path.name} writes `{field} {value}`, which is a note to "
                        "come back rather than an answer. A record is accepted or it "
                        "is not written yet.",
                    )

    def test_a_record_that_is_only_headings_fails_these_checks(self):
        """Every record in the tree passes the two tests above, and so would a tree
        the readers cannot parse. This builds the skeleton those tests exist to
        reject and requires each of them to see it -- so a `sections_of` that stops
        matching the heading style, or a `field_value` that stops finding fields,
        fails here rather than going quietly green everywhere."""
        # Titled without a number on purpose, and described without one here.
        # `CITATION` sweeps this file along with the rest of the tree, so a
        # four-digit number behind that prefix -- in the skeleton or in a comment
        # about it -- reads as a citation of a record that does not exist. Writing
        # the example out is what caught it.
        skeleton = "\n".join(
            ["# A record that says nothing", ""]
            + [f"{field} " for field in REQUIRED_FIELDS]
            + ["", *(line for heading in REQUIRED_HEADINGS for line in (heading, ""))]
        )

        self.assertEqual(
            sorted(REQUIRED_HEADINGS),
            sorted(sections_of(skeleton)),
            "sections_of no longer splits a record on its headings, so the section "
            "check reads every body as missing or every body as the whole file.",
        )

        for heading in REQUIRED_HEADINGS:
            with self.subTest(section=heading):
                self.assertLess(
                    len(sections_of(skeleton)[heading].split()),
                    MINIMUM_SECTION_WORDS,
                    f"an empty `{heading}` section counts as filled, so the check "
                    "passes on a record with nothing under its headings.",
                )

        for field in REQUIRED_FIELDS:
            with self.subTest(field=field):
                self.assertFalse(
                    field_value(skeleton, field),
                    f"an empty `{field}` reads back as a value, so the field check "
                    "passes on a header nobody filled in.",
                )

        self.assertIsNotNone(
            PLACEHOLDER.match("TBD"),
            "PLACEHOLDER no longer matches the word it was written for.",
        )

    def test_every_record_is_numbered_uniquely(self):
        numbers = [adr_number(path) for path in adr_files()]
        self.assertNotVacuous(numbers, "no records found")
        duplicates = sorted({n for n in numbers if numbers.count(n) > 1})
        self.assertEqual(
            [],
            duplicates,
            f"two records share a number: {duplicates}. A citation of `ADR-{duplicates}` "
            "would be ambiguous.",
        )

    def test_the_index_lists_every_record(self):
        index = ADR_README.read_text()
        for path in adr_files():
            with self.subTest(record=path.name):
                self.assertIn(
                    f"({path.name})",
                    index,
                    f"{path.name} is not linked from {ADR_README.name}, so it is only "
                    "reachable by listing the directory.",
                )


class CitationTests(harness.HarnessAssertions, unittest.TestCase):
    """The tree and the records point at each other, in both directions."""

    def _tree_text(self):
        """(path, text) for every tracked file that could carry a citation.

        Records themselves are excluded: ADR-0002 citing ADR-0001 would satisfy the
        `is it cited` check without a line of code depending on either."""
        searched = sorted(
            {
                *harness.tracked_under(REPO_ROOT / "Tests" / "PrTestEnvironments"),
                *harness.tracked_under(REPO_ROOT / ".github"),
                *harness.tracked_under(REPO_ROOT / "Deployment"),
                *harness.tracked_under(REPO_ROOT / "Documentation"),
            }
        )

        out = []
        for relative in searched:
            path = REPO_ROOT / relative
            if path.is_relative_to(ADR_DIR):
                continue
            try:
                out.append((relative, path.read_text()))
            except (UnicodeDecodeError, OSError):
                continue
        return out

    def test_every_citation_names_a_record_that_exists(self):
        numbers = {adr_number(path) for path in adr_files()}
        self.assertNotVacuous(numbers, "no records found")

        dangling = []
        for relative, text in self._tree_text():
            for cited in set(CITATION.findall(text)):
                if cited not in numbers:
                    dangling.append(f"{relative} cites ADR-{cited}")

        self.assertEqual(
            [],
            sorted(dangling),
            "a citation names a record that is not in Documentation/adr. Either the "
            "record was renumbered or it was never written, and the reader following "
            "the citation finds nothing.",
        )

    def test_every_record_is_cited_from_the_code_it_governs(self):
        cited = set()
        for _, text in self._tree_text():
            cited.update(CITATION.findall(text))

        self.assertNotVacuous(cited, "nothing in the tree cites any record at all")

        uncited = sorted(
            adr_number(path) for path in adr_files() if adr_number(path) not in cited
        )
        self.assertEqual(
            [],
            uncited,
            f"ADR {uncited} is not cited anywhere outside Documentation/adr. Nobody "
            "reads a directory of records on a hunch. The reasoning is only found if "
            "the code that depends on it points at the record.",
        )

    def test_the_file_each_record_names_as_its_enforcer_exists_and_cites_it(self):
        for path in adr_files():
            enforcers = cited_files_of(path)
            with self.subTest(record=path.name):
                self.assertNotVacuous(
                    enforcers,
                    f"{path.name} names no file in its `Enforced by:` field, so "
                    "nothing fails when the decision is undone.",
                )

                number = adr_number(path)
                for enforcer in enforcers:
                    enforcing_file = REPO_ROOT / enforcer
                    if not enforcing_file.exists():
                        # The field also names a class inside a file. Only check the
                        # entries that look like a path.
                        self.assertNotIn(
                            "/",
                            enforcer,
                            f"{path.name} says `{enforcer}` enforces it and no such "
                            "file exists.",
                        )
                        continue

                    self.assertIn(
                        f"ADR-{number}",
                        enforcing_file.read_text(),
                        f"{path.name} names {enforcer} as its enforcer, but that file "
                        f"never mentions ADR-{number}. The link has to run both ways "
                        "or the reader arrives at the guard without the reason.",
                    )


if __name__ == "__main__":
    unittest.main()
