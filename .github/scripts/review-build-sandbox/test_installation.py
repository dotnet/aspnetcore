import os
from pathlib import Path
import struct
import subprocess
import tempfile
import textwrap
import unittest


WORKFLOW = Path(__file__).resolve().parents[2] / "workflows" / "pull-request-review.md"


class InstallationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        source = WORKFLOW.read_text(encoding="utf-8")
        step = source.split("      - name: Install pinned standalone non-AI AWF\n", 1)[1]
        body = step.split("        run: |\n", 1)[1]
        lines = []
        for line in body.splitlines():
            if line and not line.startswith("          "):
                break
            lines.append(line)
        cls.script = textwrap.dedent("\n".join(lines))
        if not cls.script:
            raise AssertionError("The actual AWF installation script was not extracted.")

    def gate(self, curl):
        with tempfile.TemporaryDirectory(prefix="review-awf-install-") as directory:
            root = Path(directory)
            (root / "wrong-checksum-elf").write_bytes(
                b"\x7fELF\x02\x01\x01" + b"\0" * 9
                + struct.pack("<HHIQQQIHHHHHH", 2, 62, 1, 0, 0, 0, 0, 64, 0, 0, 0, 0, 0)
            )
            env = {key: os.environ[key] for key in ("PATH", "SYSTEMROOT") if key in os.environ}
            env.update({
                "HOME": root.as_posix(),
                "RUNNER_OS": "Linux",
                "RUNNER_TEMP": root.as_posix(),
                "GITHUB_PATH": (root / "github-path").as_posix(),
            })
            script = (
                "file \"$RUNNER_TEMP/wrong-checksum-elf\" | grep --quiet 'ELF.*executable' || exit 66\n"
                "uname() { printf '%s\\n' x86_64; }\n"
                "chmod() { printf '%s\\n' called > \"$RUNNER_TEMP/chmod-called\"; }\n"
                + curl + "\n" + self.script
            )
            result = subprocess.run(
                ["bash", "--noprofile", "--norc", "-c", script],
                env=env, text=True, capture_output=True, timeout=10,
            )
            return result, (root / "chmod-called").exists(), (root / "github-path").exists()

    def test_checksum_mismatch_stops_before_executable_or_path_export(self):
        result, made_executable, exported_path = self.gate(r"""
curl() {
  while (( $# )); do
    if [ "$1" = "--output" ]; then
      cp "$RUNNER_TEMP/wrong-checksum-elf" "$2"
      return 0
    fi
    shift
  done
  return 65
}
""")
        self.assertEqual(1, result.returncode, result.stderr)
        self.assertIn("FAILED", result.stdout)
        self.assertFalse(made_executable)
        self.assertFalse(exported_path)

    def test_download_failure_stops_before_executable_or_path_export(self):
        result, made_executable, exported_path = self.gate(r"""
curl() {
  printf '%s\n' 'simulated download failure' >&2
  return 23
}
""")
        self.assertEqual(23, result.returncode, result.stderr)
        self.assertIn("simulated download failure", result.stderr)
        self.assertFalse(made_executable)
        self.assertFalse(exported_path)


if __name__ == "__main__":
    unittest.main()
