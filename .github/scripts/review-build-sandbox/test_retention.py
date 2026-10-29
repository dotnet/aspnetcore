import ast
import hashlib
import json
import os
from pathlib import Path
import re
import stat
import tempfile
import unittest
from unittest.mock import patch

from contract import PROFILES
import receipt


HOST = Path(__file__).with_name("host.py")


class HostRetentionTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="review-build-salvage-")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.out = self.root / "out"
        self.evidence = self.root / "evidence"
        self.out.mkdir()
        self.evidence.mkdir()
        self.report = b'<assemblies><assembly failed="1" /></assemblies>'
        self.retention = {
            "state": "copied", "reason": "single_report",
            "source": "Microsoft.AspNetCore.ResponseCaching.Tests_net12.0_x64.xml",
            "file_sha256": {"tests.xml": hashlib.sha256(self.report).hexdigest()},
        }
        self.scope = {
            "os": os, "stat": stat, "hashlib": hashlib, "json": json, "re": re,
            "OUT": self.out, "EVIDENCE": self.evidence, "PROFILE": PROFILES[0],
            "receipt": {"status": "failed", "error": "AWF/target failed: exit 1"},
        }
        if os.name == "nt":
            for name in ("O_NOFOLLOW", "O_NONBLOCK"):
                replacement = patch.object(os, name, 0, create=True)
                replacement.start()
                self.addCleanup(replacement.stop)
        module = ast.parse(HOST.read_text(encoding="utf-8"))
        functions = [
            node for node in module.body
            if isinstance(node, ast.FunctionDef) and node.name in ("bounded", "salvage_outputs")
        ]
        exec(compile(ast.Module(body=functions, type_ignores=[]), str(HOST), "exec"), self.scope)
        finalizer = next(node.finalbody for node in module.body if isinstance(node, ast.Try))
        self.original_salvage = next((
            node for node in ast.walk(ast.Module(body=finalizer, type_ignores=[]))
            if isinstance(node, ast.For) and isinstance(node.target, ast.Name) and node.target.id == "name"
        ), None)

    def salvage(self):
        if "salvage_outputs" in self.scope:
            self.scope["salvage_outputs"]()
        else:
            exec(compile(ast.Module(body=[self.original_salvage], type_ignores=[]), str(HOST), "exec"), self.scope)

    def seed(self, retention=None):
        (self.out / "tests.xml").write_bytes(self.report)
        (self.out / "retention.json").write_text(json.dumps(
            self.retention if retention is None else retention,
        ), encoding="utf-8")

    def test_allowed_failed_xml_and_retention_are_salvaged_without_changing_failure(self):
        self.seed()
        for name in ("unrelated.xml", "unselected.trx", "instructions.md"):
            (self.out / name).write_bytes(b"not selected evidence")
        self.salvage()
        self.assertEqual({"tests.xml", "retention.json"}, {path.name for path in self.evidence.iterdir()})
        self.assertEqual(self.report, (self.evidence / "tests.xml").read_bytes())
        self.assertEqual(self.retention, self.scope["receipt"].get("retention"))
        self.assertEqual("failed", self.scope["receipt"]["status"])
        self.assertEqual("AWF/target failed: exit 1", self.scope["receipt"]["error"])
        report = {"status": "failed", "file_sha256": {
            path.name: hashlib.sha256(path.read_bytes()).hexdigest() for path in self.evidence.iterdir()
        }}
        receipt.verify_files(report, self.evidence)

    def test_failed_or_rejected_retention_never_exports_partial_xml(self):
        for state in ("absent", "malformed_or_truncated", "rejected", "copy_failed"):
            with self.subTest(state=state):
                retained = {"state": state, "reason": "test_reason", "file_sha256": {}}
                self.seed(retained)
                self.salvage()
                self.assertEqual(retained, self.scope["receipt"].get("retention"))
                self.assertFalse((self.evidence / "tests.xml").exists())
                self.assertEqual("AWF/target failed: exit 1", self.scope["receipt"]["error"])
                (self.evidence / "retention.json").unlink()

    def test_hash_mismatch_is_rejected_before_export(self):
        self.seed()
        (self.out / "tests.xml").write_bytes(b"changed after guest retention")
        self.salvage()
        self.assertFalse((self.evidence / "tests.xml").exists())
        self.assertTrue(any("hash mismatch" in error for error in self.scope["receipt"]["rejected_outputs"]))
        self.assertEqual("failed", self.scope["receipt"]["status"])
        self.assertEqual("rejected", self.scope["receipt"].get("retention", {}).get("state"))

    def test_malformed_or_oversized_retention_metadata_rejects_xml(self):
        for case, data in (
            ("truncated", b"{"), ("oversized", b" " * 4097),
            ("unknown-state", b'{"state":"unexpected","reason":"test","file_sha256":{}}'),
            ("recursive", b"[" * 1500 + b"]" * 1500),
        ):
            with self.subTest(case=case):
                self.seed()
                (self.out / "retention.json").write_bytes(data)
                self.salvage()
                self.assertEqual("rejected", self.scope["receipt"].get("retention", {}).get("state"))
                self.assertFalse((self.evidence / "tests.xml").exists())
                self.assertFalse((self.evidence / "retention.json").exists())

    def test_source_path_escape_is_rejected(self):
        self.seed({**self.retention, "source": "../outside.xml"})
        self.salvage()
        self.assertEqual("rejected", self.scope["receipt"].get("retention", {}).get("state"))
        self.assertFalse((self.evidence / "tests.xml").exists())

    def test_successful_report_without_failure_metadata_is_unchanged(self):
        self.scope["receipt"] = {"status": "passed"}
        (self.out / "tests.xml").write_bytes(self.report)
        self.salvage()
        self.assertEqual({"tests.xml"}, {path.name for path in self.evidence.iterdir()})
        self.assertEqual(self.report, (self.evidence / "tests.xml").read_bytes())
        self.assertEqual({"status": "passed"}, self.scope["receipt"])

    def test_failed_report_without_metadata_is_not_exported(self):
        (self.out / "tests.xml").write_bytes(self.report)
        self.salvage()
        self.assertFalse((self.evidence / "tests.xml").exists())
        self.assertEqual({
            "state": "absent", "reason": "missing_retention_metadata", "file_sha256": {},
        }, self.scope["receipt"].get("retention"))
        self.assertEqual("AWF/target failed: exit 1", self.scope["receipt"]["error"])

    def test_missing_copied_report_is_recorded_as_absent(self):
        self.seed()
        (self.out / "tests.xml").unlink()
        self.salvage()
        self.assertFalse((self.evidence / "tests.xml").exists())
        self.assertEqual({
            "state": "absent", "reason": "missing_retained_report", "file_sha256": {},
        }, self.scope["receipt"].get("retention"))


if __name__ == "__main__":
    unittest.main()
