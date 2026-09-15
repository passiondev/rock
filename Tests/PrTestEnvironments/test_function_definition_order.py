"""A PowerShell script runs top to bottom, so a function called before its
definition is not defined yet.

`Deploy-RockEnvironment.ps1` defines `Resolve-DeploymentTarget` at file scope and
calls it at file scope eighty lines later. Move the definition below the call --
a reasonable-looking tidy-up, grouping the helpers at the bottom -- and the
script parses cleanly, passes every Pester test, and dies on the VM with "not
recognized as the name of a cmdlet". Pester cannot see it: it loads one function
out of the file and runs it in isolation, which is exactly the arrangement that
makes the ordering irrelevant. Neither can the parser, because the file is
syntactically fine.

The check is possible at all because these scripts have a shape: every function
is defined at column 0, and every file-scope statement is at column 0 too.
Anything indented is inside something and runs later. That is a convention rather
than a language rule, so the first test here holds the convention and the second
depends on it.
"""

import re
import unittest

import pipeline_harness as harness


SCRIPT_DIRS = [
    harness.REPO_ROOT / "Deployment" / "PrTestEnvironments",
    harness.REPO_ROOT / "Deployment" / "Repository",
    harness.REPO_ROOT / ".github" / "actions",
]

DEFINITION = re.compile(r"^function\s+([A-Za-z][\w-]*)", re.MULTILINE)
INDENTED_DEFINITION = re.compile(r"^[ \t]+function\s+[A-Za-z][\w-]*")


def scripts():
    """Every PowerShell script under the directories that hold deployable code."""
    found = []
    for directory in SCRIPT_DIRS:
        if directory.exists():
            found.extend(sorted(directory.rglob("*.ps1")))
    return found


def code_lines(text):
    """(line number, text) for every line that a runner will execute.

    Block comments, line comments and here-string bodies are dropped, and the
    indentation of what survives is kept. A here-string can hold anything at all
    -- SQL, another script -- and matching a function name inside one would
    report a call that is really just a word in a string.

    Both tests below ask this rather than the raw file, and for the same reason
    in both directions: a comment can say anything. The convention test used to
    regex the raw text and reported `Deploy-RockEnvironment.ps1:2194 function and`
    -- a line of .DESCRIPTION prose that happened to wrap onto the word. Nothing
    is defined inside a comment, and nothing is called from one either.
    """
    lines = []
    in_block_comment = False
    here_string_terminator = None

    for number, line in enumerate(text.splitlines(), 1):
        if here_string_terminator is not None:
            if line.startswith(here_string_terminator):
                here_string_terminator = None
            continue

        if in_block_comment:
            if "#>" in line:
                in_block_comment = False
            continue

        if line.lstrip().startswith("<#"):
            in_block_comment = "#>" not in line
            continue

        opener = re.search(r"@(['\"])\s*$", line)
        if opener:
            here_string_terminator = opener.group(1) + "@"

        code = line.split("#", 1)[0] if not line.lstrip().startswith("#") else ""
        if code.strip():
            lines.append((number, code))

    return lines


def executable_lines(text):
    """The subset of `code_lines` at column 0, which is this file's file scope."""
    return [(number, code) for number, code in code_lines(text) if not code[0].isspace()]


def indented_definitions(text):
    """`line number, declaration` for each function defined below file scope."""
    return [
        (number, code.strip())
        for number, code in code_lines(text)
        if code[0].isspace() and INDENTED_DEFINITION.match(code)
    ]


class ColumnZeroConventionTests(harness.HarnessAssertions, unittest.TestCase):
    def test_an_indented_definition_is_still_caught(self):
        """Calibration for the test below, which reports an empty list on a clean
        tree and would report the same list if the scan had stopped working."""
        planted = "function Outer {\n    function Inner {\n    }\n}\n"
        self.assertEqual([(2, "function Inner {")], indented_definitions(planted))

        commented = "<#\n    function Inner\n#>\nfunction Outer {\n}\n"
        self.assertEqual(
            [], indented_definitions(commented),
            "comment prose is being read as a definition, which is the false "
            "positive dropping the raw-text scan was for",
        )

    def test_no_function_is_defined_indented(self):
        """The ordering check below reads column 0 as file scope. A function defined
        inside another one breaks that reading, and would be reported as a call."""
        offenders = []
        for path in scripts():
            for line, declaration in indented_definitions(path.read_text(encoding="utf-8")):
                offenders.append(f"{path.name}:{line} {declaration}")

        self.assertEqual(
            [],
            offenders,
            "these define a function somewhere other than file scope, which is "
            "outside what test_function_definition_order.py can reason about:\n  "
            + "\n  ".join(offenders),
        )


class DefinitionPrecedesUseTests(harness.HarnessAssertions, unittest.TestCase):
    def test_every_file_scope_call_comes_after_its_definition(self):
        checked = []
        offenders = []

        for path in scripts():
            text = path.read_text(encoding="utf-8")
            defined = {}
            for match in DEFINITION.finditer(text):
                defined.setdefault(match.group(1), harness.line_of(text, match.start()))
            if not defined:
                continue

            callable_names = re.compile(
                r"(?<![\w-])(" + "|".join(re.escape(name) for name in defined) + r")(?![\w-])"
            )

            for number, code in executable_lines(text):
                if code.startswith("function "):
                    continue
                for name in set(callable_names.findall(code)):
                    checked.append(f"{path.name}:{name}")
                    if number < defined[name]:
                        offenders.append(
                            f"{path.name}:{number} calls {name}, defined at line {defined[name]}"
                        )

        self.assertNotVacuous(checked, "no file-scope call to a locally defined function was found")
        self.assertEqual(
            [],
            offenders,
            "these call a function before the line that defines it, so the script "
            "parses, passes Pester, and fails on the VM:\n  " + "\n  ".join(offenders),
        )


if __name__ == "__main__":
    unittest.main()
