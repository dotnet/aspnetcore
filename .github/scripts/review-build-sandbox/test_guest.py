import ast
import hashlib
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

import contract


GUEST = Path(__file__).with_name("guest.py")


class GuestTests(unittest.TestCase):
    def run_guest(self, outcome, report_count=1, failure_phase="test", report_kind="file",
                  report_data=None, copy_failure=False, stale_report=None, timeout=False,
                  metadata_failure=False, earlier_report=False):
        with tempfile.TemporaryDirectory(prefix="review-build-guest-") as directory:
            root = Path(directory)
            source = root / "work"
            out = root / "out"
            control = root / "control"
            for path in (source, out, control):
                path.mkdir()
            profile = contract.PROFILES[0]
            (control / "profile.json").write_text(json.dumps(profile), encoding="utf-8")
            report = (
                '<assemblies><assembly total="2" passed="1" failed="1" skipped="0">'
                '<test name="unchanged-row" result="Pass" />'
                '<test name="intended-row" result="Fail" />'
                '</assembly></assemblies>'
                if outcome else
                '<assemblies><assembly total="2" passed="2" failed="0" skipped="0">'
                '<test name="unchanged-row" result="Pass" />'
                '<test name="intended-row" result="Pass" />'
                '</assembly></assemblies>'
            ).encode("utf-8")
            if report_data is not None:
                report = report_data
            reports = source / "artifacts" / "TestResults" / "Debug"
            if stale_report is not None:
                reports.mkdir(parents=True)
                (reports / f"{profile['assembly']}_net12.0_x64.xml").write_bytes(stale_report)
            if copy_failure:
                (out / "tests.xml").mkdir()
            if metadata_failure:
                (out / "retention.json").mkdir()
            planned = {
                name: [sys.executable, "-c", "pass"]
                for name in contract.commands(profile)
            }
            producer = [
                "import os, pathlib, sys, time",
                f"reports = pathlib.Path({str(reports)!r})",
                "reports.mkdir(parents=True, exist_ok=True)",
            ]
            if report_kind == "directory_symlink":
                producer.extend([
                    f"outside = pathlib.Path({str(root / 'outside-results')!r})",
                    "outside.mkdir()",
                    "reports.rmdir()",
                    "os.symlink(outside, reports, target_is_directory=True)",
                ])
            if report_kind == "entry_limit":
                producer.append("[(reports / str(index)).touch() for index in range(129)]")
            for index in range(report_count):
                filename = f"{profile['assembly']}_net{12 + index}.0_x64.xml"
                producer.append(f"path = reports / {filename!r}")
                if report_kind == "symlink":
                    producer.extend([
                        f"outside = pathlib.Path({str(root / 'outside.xml')!r})",
                        f"outside.write_bytes({report!r})",
                        "os.symlink(outside, path)",
                    ])
                elif report_kind == "oversize":
                    producer.append("path.open('wb').truncate(8 * 1024 * 1024 + 1)")
                elif report_kind == "directory":
                    producer.append("path.mkdir()")
                elif report_kind == "hardlink":
                    producer.extend([
                        f"outside = pathlib.Path({str(root / 'outside.xml')!r})",
                        f"outside.write_bytes({report!r})",
                        "os.link(outside, path)",
                    ])
                elif report_kind == "unrelated":
                    producer.append(f"(reports / 'unrelated.xml').write_bytes({report!r})")
                else:
                    producer.append(f"path.write_bytes({report!r})")
            if timeout:
                producer.append("time.sleep(10)")
            producer.append(f"sys.exit({outcome})")
            planned[failure_phase] = [sys.executable, "-c", "; ".join(producer)]
            if earlier_report:
                planned["build"] = [
                    sys.executable, "-c",
                    f"import pathlib; p = pathlib.Path({str(reports)!r}); "
                    f"p.mkdir(parents=True, exist_ok=True); (p / {filename!r}).write_bytes({report!r})",
                ]
            module = ast.parse(GUEST.read_text(encoding="utf-8"))
            runner = module.body[-1]
            self.assertIsInstance(runner, ast.Try)
            scope = {"__name__": "guest_test"}
            with patch.object(contract, "ROOT", str(root)), patch.object(contract, "commands", return_value=planned), patch.dict(os.environ):
                exec(compile(ast.Module(body=module.body[:-1], type_ignores=[]), str(GUEST), "exec"), scope)
                scope["preflight"] = lambda: None
                if timeout:
                    actual_phase = scope["phase"]
                    scope["phase"] = lambda name, command, seconds: actual_phase(
                        name, command, 0.5 if name == "test" else seconds,
                    )
                try:
                    exec(compile(ast.Module(body=[runner], type_ignores=[]), str(GUEST), "exec"), scope)
                except SystemExit as error:
                    exit_code = error.code
                else:
                    exit_code = 0
            return {
                "exit": exit_code,
                "report": (out / "tests.xml").read_bytes() if (out / "tests.xml").is_file() else None,
                "expected": report,
                "phases": json.loads((out / "phases.json").read_text()) if (out / "phases.json").exists() else [],
                "failure": (out / "failure.txt").read_text() if (out / "failure.txt").exists() else None,
                "retention": json.loads((out / "retention.json").read_text()) if (out / "retention.json").is_file() else None,
            }

    def test_failed_tests_preserve_report_and_nonzero_exit(self):
        result = self.run_guest(7)
        self.assertEqual(1, result["exit"])
        self.assertEqual(result["expected"], result["report"])
        self.assertEqual(
            [("unchanged-row", "Pass"), ("intended-row", "Fail")],
            [(test.get("name"), test.get("result")) for test in ET.fromstring(result["report"]).findall(".//test")],
        )
        self.assertEqual([0, 0, 0, 0, 7], [phase["exit"] for phase in result["phases"]])
        self.assertEqual("RuntimeError: test failed: exit 7\n", result["failure"])
        self.assertEqual({
            "state": "copied", "reason": "single_report",
            "source": "Microsoft.AspNetCore.ResponseCaching.Tests_net12.0_x64.xml",
            "file_sha256": {"tests.xml": hashlib.sha256(result["expected"]).hexdigest()},
        }, result["retention"])

    def test_successful_tests_preserve_report_and_exit_zero(self):
        result = self.run_guest(0)
        self.assertEqual(0, result["exit"])
        self.assertEqual(result["expected"], result["report"])
        self.assertEqual([0, 0, 0, 0, 0], [phase["exit"] for phase in result["phases"]])
        self.assertIsNone(result["failure"])
        self.assertIsNone(result["retention"])

    def test_failed_tests_without_report_keep_phase_failure(self):
        result = self.run_guest(7, report_count=0)
        self.assertEqual(1, result["exit"])
        self.assertIsNone(result["report"])
        self.assertEqual("RuntimeError: test failed: exit 7\n", result["failure"])
        self.assertEqual(7, result["phases"][-1]["exit"])
        self.assertEqual({"state": "absent", "reason": "no_report", "file_sha256": {}}, result["retention"])

    def test_failure_before_tests_does_not_export_report(self):
        result = self.run_guest(7, failure_phase="build")
        self.assertEqual(1, result["exit"])
        self.assertIsNone(result["report"])
        self.assertEqual(["bootstrap", "sdk", "restore", "build"], [phase["name"] for phase in result["phases"]])
        self.assertEqual("RuntimeError: build failed: exit 7\n", result["failure"])

    def test_success_without_report_fails(self):
        result = self.run_guest(0, report_count=0)
        self.assertEqual(1, result["exit"])
        self.assertIsNone(result["report"])
        self.assertEqual("RuntimeError: Expected one test report for the selected assembly\n", result["failure"])

    def test_ambiguous_reports_are_not_exported(self):
        for outcome in (0, 7):
            with self.subTest(outcome=outcome):
                result = self.run_guest(outcome, report_count=2)
                self.assertEqual(1, result["exit"])
                self.assertIsNone(result["report"])
                self.assertEqual(outcome, result["phases"][-1]["exit"])
                self.assertEqual(
                    "RuntimeError: test failed: exit 7\n" if outcome else
                    "RuntimeError: Expected one test report for the selected assembly\n",
                    result["failure"],
                )
                if outcome:
                    self.assertEqual({
                        "state": "rejected", "reason": "multiple_reports", "file_sha256": {},
                    }, result["retention"])

    def test_failed_report_copy_error_preserves_original_failure(self):
        result = self.run_guest(7, copy_failure=True)
        self.assertEqual(1, result["exit"])
        self.assertIsNone(result["report"])
        self.assertEqual("RuntimeError: test failed: exit 7\n", result["failure"])
        self.assertEqual({
            "state": "copy_failed", "reason": "PermissionError" if os.name == "nt" else "FileExistsError",
            "file_sha256": {},
        }, result["retention"])

    def test_failed_malformed_or_truncated_report_is_distinguished(self):
        for case, data in (("empty", b""), ("truncated", b"<assemblies><assembly>")):
            with self.subTest(case=case):
                result = self.run_guest(7, report_data=data)
                self.assertEqual(1, result["exit"])
                self.assertIsNone(result["report"])
                self.assertEqual("RuntimeError: test failed: exit 7\n", result["failure"])
                self.assertEqual({
                    "state": "malformed_or_truncated", "reason": "xml_parse", "file_sha256": {},
                }, result["retention"])

    def test_failed_oversize_or_linked_report_is_rejected(self):
        for kind, reason in (
            ("oversize", "size_limit"), ("symlink", "linked_or_non_regular"),
            ("directory", "linked_or_non_regular"), ("hardlink", "linked_or_non_regular"),
            ("directory_symlink", "linked_or_non_directory"), ("entry_limit", "directory_entry_limit"),
        ):
            with self.subTest(kind=kind):
                result = self.run_guest(7, report_kind=kind)
                self.assertEqual(1, result["exit"])
                self.assertIsNone(result["report"])
                self.assertEqual("RuntimeError: test failed: exit 7\n", result["failure"])
                self.assertEqual({"state": "rejected", "reason": reason, "file_sha256": {}}, result["retention"])

    def test_unrelated_xml_is_not_exported(self):
        result = self.run_guest(7, report_kind="unrelated")
        self.assertEqual(1, result["exit"])
        self.assertIsNone(result["report"])
        self.assertEqual({"state": "absent", "reason": "no_report", "file_sha256": {}}, result["retention"])

    def test_test_timeout_preserves_report_and_timeout_failure(self):
        result = self.run_guest(7, timeout=True)
        self.assertEqual(1, result["exit"])
        self.assertEqual(result["expected"], result["report"])
        self.assertEqual(124, result["phases"][-1]["exit"])
        self.assertEqual("RuntimeError: test failed: exit 124\n", result["failure"])
        self.assertEqual("copied", result["retention"]["state"])

    def test_consecutive_runs_cannot_reuse_stale_test_results(self):
        first = self.run_guest(0)
        self.assertEqual(0, first["exit"])
        second = self.run_guest(0, report_count=0, stale_report=first["report"])
        self.assertEqual(1, second["exit"])
        self.assertEqual(["bootstrap", "sdk", "restore", "build"], [phase["name"] for phase in second["phases"]])
        self.assertIsNone(second["report"])
        self.assertEqual(
            "RuntimeError: Test result directory is not empty before test execution\n", second["failure"],
        )

    def test_build_report_cannot_be_reused_as_test_output(self):
        result = self.run_guest(7, earlier_report=True)
        self.assertEqual(1, result["exit"])
        self.assertEqual(["bootstrap", "sdk", "restore", "build"], [phase["name"] for phase in result["phases"]])
        self.assertIsNone(result["report"])
        self.assertIsNone(result["retention"])
        self.assertEqual(
            "RuntimeError: Test result directory is not empty before test execution\n", result["failure"],
        )

    def test_metadata_write_failure_preserves_original_test_failure(self):
        result = self.run_guest(7, metadata_failure=True)
        self.assertEqual(1, result["exit"])
        self.assertEqual(7, result["phases"][-1]["exit"])
        self.assertEqual("RuntimeError: test failed: exit 7\n", result["failure"])
        self.assertIsNone(result["retention"])
        self.assertEqual(result["expected"], result["report"])

    def test_xml_declarations_are_rejected_without_replacing_failure(self):
        for case, data in (
            ("doctype", b"<!DOCTYPE assemblies><assemblies />"),
            ("entity", b'<!ENTITY value "test"><assemblies />'),
            ("nul", b"<assemblies />\0"),
        ):
            with self.subTest(case=case):
                result = self.run_guest(7, report_data=data)
                self.assertEqual(1, result["exit"])
                self.assertIsNone(result["report"])
                self.assertEqual("RuntimeError: test failed: exit 7\n", result["failure"])
                self.assertEqual({
                    "state": "rejected", "reason": "xml_declaration", "file_sha256": {},
                }, result["retention"])


if __name__ == "__main__":
    unittest.main()
