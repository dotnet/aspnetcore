from pathlib import Path
import re
import subprocess
import unittest


REPOSITORY = Path(__file__).resolve().parents[3]
WORKFLOW = REPOSITORY / ".github" / "workflows" / "workflow-tests.yml"
SANDBOX = ".github/scripts/review-build-sandbox/"
PUBLICATION_PREFLIGHT = ".github/workflows/tests/pull-request-review-publication-preflight.test.mjs"


class ReviewBuildSandboxCiTests(unittest.TestCase):
    def test_sandbox_changes_trigger_workflow_tests(self):
        source = WORKFLOW.read_text(encoding="utf-8")
        pull_request = source.split("  pull_request:\n", 1)[1].split("\n\n", 1)[0]
        self.assertIn("      - '.github/scripts/review-build-sandbox/**'", pull_request)

    def test_actual_discovery_includes_sandbox_tests_but_not_runtime_modules(self):
        source = WORKFLOW.read_text(encoding="utf-8")
        shopt = re.search(r"^\s+(shopt [^\n]+)$", source, re.M)
        discovery = re.search(r"^\s+(tests=\([^\n]+\))$", source, re.M)
        self.assertIsNotNone(shopt)
        self.assertIsNotNone(discovery)
        result = subprocess.run(
            ["bash", "--noprofile", "--norc", "-c",
             shopt[1] + "\n" + discovery[1] + '\nprintf \'%s\\n\' "${tests[@]}"'],
            cwd=REPOSITORY, text=True, capture_output=True, check=True, timeout=10,
        )
        discovered = set(result.stdout.splitlines())
        expected = {
            path.relative_to(REPOSITORY).as_posix()
            for path in (REPOSITORY / SANDBOX).glob("test_*.py")
        }
        self.assertTrue(expected)
        self.assertEqual(expected, {path for path in discovered if path.startswith(SANDBOX)})
        self.assertIn(PUBLICATION_PREFLIGHT, discovered)
        self.assertRegex(source, r"\*\.js\|\*\.cjs\|\*\.mjs\) node ")
        for module in ("contract.py", "guest.py", "host.py", "receipt.py"):
            with self.subTest(module=module):
                self.assertNotIn(SANDBOX + module, discovered)


if __name__ == "__main__":
    unittest.main()
