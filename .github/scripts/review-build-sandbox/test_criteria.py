import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest

import criteria


class CriteriaTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.bundle = self.root / "bundle"
        self.workspace = self.root / "workspace"
        self.workspace.mkdir()
        self.guides = {
            "docs/First.md": "---\nname: first\n---\n# Complete guide\nGetType\nend-first",
            "docs/Second.md": "# Second\r\ncaf\u00e9 e\u0301\r\nlast-line\r\n",
        }
        for name, body in self.guides.items():
            path = self.bundle / "guidance" / (name + ".source")
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(body.encode("utf-8"))
        self.manifest = {
            "version": 2, "ready": True, "suffix": ".source",
            "target": {"head": "a" * 40, "mergeBase": "b" * 40, "baseTip": "c" * 40},
            "guidance": {"root": "guidance", "commit": "d" * 40},
            "guides": [{"path": name} for name in self.guides],
            "policies": [
                {"guide": "docs/First.md", "path": "docs/Policy.md", "anchor": "first",
                 "body": "## First\nCall `GetType`, not `Type.GetType`.\nfinal-first"},
                {"guide": "docs/Second.md", "path": "docs/Policy.md", "anchor": "second",
                 "body": "## Second\r\nKeep \u00e9 and e\u0301 distinct.\r\n"},
            ],
            "exclusions": [
                {"scope": "Target execution", "reason": "Source-only reviewers."},
                {"source": ".github/copilot-instructions.md#scope", "body": "## Scope\nExact exclusion.\n"},
            ],
            "pull": {"title": "UNTRUSTED_TITLE_MUST_NOT_ENTER_NATIVE_INSTRUCTIONS"},
        }
        self.manifest_path = self.bundle / "manifest.json"
        self.destination = self.workspace / ".github" / "copilot-instructions.md"

    def materialize(self):
        self.manifest_path.write_bytes(json.dumps(self.manifest, ensure_ascii=True).encode("utf-8"))
        return criteria.materialize(self.manifest_path, self.workspace, "a" * 40, "d" * 40)

    def assert_rejected(self, message):
        with self.assertRaisesRegex((ValueError, UnicodeError, OSError), message):
            self.materialize()
        self.assertFalse(self.destination.exists())

    def test_complete_ordered_bodies_and_identity_survive_without_normalization(self):
        result = self.materialize()
        content = self.destination.read_bytes()
        cursor = 0
        bodies = [*self.guides.values(), *(policy["body"] for policy in self.manifest["policies"]),
                  "", self.manifest["exclusions"][1]["body"]]
        self.assertEqual(6, len(result["criteria"]))
        for item, body in zip(result["criteria"], bodies, strict=True):
            with self.subTest(kind=item["kind"], index=cursor):
                metadata = {key: value for key, value in item.items() if key != "kind"}
                if item["kind"] == "EXCLUSION":
                    metadata.update({key: value for key, value in self.manifest["exclusions"][item["index"]].items()
                                     if key != "body"})
                marker = (item["kind"] + " " + json.dumps(metadata, sort_keys=True)).encode("ascii")
                begin = b"\nBEGIN_" + marker + b"\n"
                start = content.index(begin, cursor) + len(begin)
                encoded = body.encode("utf-8")
                end = start + item["utf8Bytes"]
                self.assertEqual(encoded, content[start:end])
                self.assertEqual(hashlib.sha256(encoded).hexdigest(), item["sha256"])
                suffix = b"\nEND_" + marker + b"\n"
                self.assertEqual(suffix, content[end:end + len(suffix)])
                cursor = end + len(suffix)
        self.assertTrue(content.endswith(("\nEND_FROZEN_REVIEW_CRITERIA_" +
                                        result["manifestSha256"] + "\n").encode("ascii")))
        self.assertEqual(hashlib.sha256(self.manifest_path.read_bytes()).hexdigest(), result["manifestSha256"])
        self.assertEqual(hashlib.sha256(content).hexdigest(), result["sha256"])
        self.assertEqual(len(content), result["utf8Bytes"])
        self.assertIn(('"head": "' + "a" * 40 + '"').encode("ascii"), content)
        self.assertIn(('"guidance": "' + "d" * 40 + '"').encode("ascii"), content)
        self.assertNotIn(self.manifest["pull"]["title"].encode("ascii"), content)

    def test_policy_associations_remain_explicit_for_each_guide(self):
        result = self.materialize()
        policies = [item for item in result["criteria"] if item["kind"] == "POLICY"]
        self.assertEqual([(0, "docs/First.md", "first"), (1, "docs/Second.md", "second")],
                         [(item["index"], item["guide"], item["anchor"]) for item in policies])
        self.assertTrue(all(item["path"] == "docs/Policy.md" for item in policies))
        exclusions = [item for item in result["criteria"] if item["kind"] == "EXCLUSION"]
        self.assertNotIn("reason", exclusions[0])
        self.assertEqual(".github/copilot-instructions.md#scope", exclusions[1]["source"])

    def test_stdout_summary_contains_only_ids_sizes_and_hashes(self):
        result = self.materialize()
        self.assertEqual({"path", "manifestSha256", "utf8Bytes", "sha256", "criteria"}, set(result))
        allowed = {"kind", "index", "path", "guide", "anchor", "source", "utf8Bytes", "sha256"}
        for index, item in enumerate(result["criteria"]):
            with self.subTest(index=index):
                self.assertLessEqual(set(item), allowed)
        serialized = json.dumps(result, sort_keys=True)
        forbidden = [
            *self.guides.values(), *(policy["body"] for policy in self.manifest["policies"]),
            self.manifest["exclusions"][0]["reason"], self.manifest["exclusions"][0]["scope"],
            self.manifest["exclusions"][1]["body"], self.manifest["pull"]["title"],
        ]
        for index, value in enumerate(forbidden):
            with self.subTest(forbidden_index=index):
                self.assertNotIn(json.dumps(value), serialized)

    def test_materialization_preserves_every_bundle_and_existing_workspace_file(self):
        self.manifest_path.write_text(json.dumps(self.manifest), encoding="utf-8")
        (self.workspace / ".github").mkdir()
        unrelated = self.workspace / ".github" / "existing.txt"
        unrelated.write_bytes(b"preserved\r\n")
        before = {path.relative_to(self.bundle): path.read_bytes()
                  for path in self.bundle.rglob("*") if path.is_file()}
        criteria.materialize(self.manifest_path, self.workspace, "a" * 40, "d" * 40)
        after = {path.relative_to(self.bundle): path.read_bytes()
                 for path in self.bundle.rglob("*") if path.is_file()}
        self.assertEqual(before, after)
        self.assertEqual(b"preserved\r\n", unrelated.read_bytes())
        self.assertEqual({Path(".github/existing.txt"), Path(".github/copilot-instructions.md")},
                         {path.relative_to(self.workspace) for path in self.workspace.rglob("*") if path.is_file()})

    def test_empty_routing_preserves_the_generic_path(self):
        self.manifest.update(guides=[], policies=[])
        result = self.materialize()
        self.assertEqual(["EXCLUSION", "EXCLUSION"], [item["kind"] for item in result["criteria"]])

    def test_existing_destination_is_never_changed(self):
        self.destination.parent.mkdir()
        self.destination.write_bytes(b"existing instructions\r\n")
        with self.assertRaises(FileExistsError):
            self.materialize()
        self.assertEqual(b"existing instructions\r\n", self.destination.read_bytes())

    def test_not_ready_wrong_version_or_stale_revisions_cannot_create_instructions(self):
        original = copy.deepcopy(self.manifest)
        for name, section, field, value in (
            ("not-ready", None, "ready", False), ("integer-ready", None, "ready", 1),
            ("old-version", None, "version", 1), ("float-version", None, "version", 2.0),
            ("stale-head", "target", "head", "e" * 40),
            ("stale-guidance", "guidance", "commit", "e" * 40),
            ("layout-root", "guidance", "root", "../guidance"),
            ("layout-suffix", None, "suffix", ".md"),
        ):
            with self.subTest(case=name):
                self.manifest = copy.deepcopy(original)
                owner = self.manifest if section is None else self.manifest[section]
                owner[field] = value
                self.assert_rejected("manifest|revisions|layout")

    def test_unsafe_guide_paths_cannot_read_outside_the_bundle(self):
        for name, value in (
            ("parent", "../outside.md"), ("absolute", "/outside.md"),
            ("windows-drive", "C:/outside.md"), ("backslash", "docs\\First.md"),
            ("double-separator", "docs//First.md"), ("dot", "docs/./First.md"),
            ("control", "docs/\x00First.md"),
        ):
            with self.subTest(case=name):
                self.manifest["guides"][0]["path"] = value
                self.assert_rejected("path")

    def test_missing_empty_or_invalid_utf8_guide_cannot_create_instructions(self):
        path = self.bundle / "guidance" / "docs" / "First.md.source"
        for name, content in (("empty", b""), ("invalid-utf8", b"\xff")):
            with self.subTest(case=name):
                path.write_bytes(content)
                self.assert_rejected("empty|utf-8")
        path.unlink()
        self.assert_rejected("find|exist|No such file")

    def test_invalid_policy_metadata_or_body_cannot_create_instructions(self):
        original = copy.deepcopy(self.manifest)
        for name, field, value in (
            ("unknown-guide", "guide", "docs/Unrouted.md"), ("guide-type", "guide", []),
            ("body-type", "body", 42),
            ("empty-body", "body", ""), ("empty-anchor", "anchor", ""),
            ("absolute-policy", "path", "/outside.md"),
        ):
            with self.subTest(case=name):
                self.manifest = copy.deepcopy(original)
                self.manifest["policies"][0][field] = value
                self.assert_rejected("policy|path")

    def test_duplicate_guides_or_malformed_collections_are_rejected(self):
        original = copy.deepcopy(self.manifest)
        for name, field, value in (
            ("duplicate-guide", "guides", [original["guides"][0], original["guides"][0]]),
            ("guide-type", "guides", [None]), ("policies-type", "policies", {}),
            ("exclusion-type", "exclusions", [None]),
            ("exclusion-field", "exclusions", [{"unexpected": "value"}]),
        ):
            with self.subTest(case=name):
                self.manifest = copy.deepcopy(original)
                self.manifest[field] = value
                self.assert_rejected("guide|array|exclusion")

    def test_mutable_or_malformed_expected_revisions_are_rejected(self):
        self.manifest_path.write_text(json.dumps(self.manifest), encoding="utf-8")
        for name, head, guidance in (
            ("branch-head", "main", "d" * 40), ("non-string-head", None, "d" * 40),
            ("branch-guidance", "a" * 40, "main"), ("uppercase-guidance", "a" * 40, "D" * 40),
        ):
            with self.subTest(case=name):
                with self.assertRaisesRegex(ValueError, "immutable"):
                    criteria.materialize(self.manifest_path, self.workspace, head, guidance)
                self.assertFalse(self.destination.exists())

    def test_reserved_outer_boundary_cannot_be_embedded_in_a_body(self):
        manifest_bytes = json.dumps(self.manifest, ensure_ascii=True).encode("utf-8")
        boundary = "FROZEN_REVIEW_CRITERIA_" + hashlib.sha256(manifest_bytes).hexdigest()
        (self.bundle / "guidance" / "docs" / "First.md.source").write_bytes(boundary.encode("ascii"))
        self.assert_rejected("reserved outer boundary")

    def test_linked_guide_directory_and_destination_are_rejected_without_writes(self):
        outside = self.root / "outside"
        outside.mkdir()
        (outside / "Escaped.md.source").write_bytes(b"must not be read")
        linked = self.bundle / "guidance" / "linked"
        linked.symlink_to(outside, target_is_directory=True)
        self.manifest["guides"][0]["path"] = "linked/Escaped.md"
        self.assert_rejected("links")
        linked.unlink()
        self.manifest["guides"][0]["path"] = "docs/First.md"
        self.destination.parent.symlink_to(outside, target_is_directory=True)
        self.assert_rejected("link")
        self.destination.parent.unlink()
        self.destination.parent.mkdir()
        target = outside / "original.md"
        target.write_bytes(b"unchanged")
        self.destination.symlink_to(target)
        with self.assertRaises(FileExistsError):
            self.materialize()
        self.assertTrue(self.destination.is_symlink())
        self.assertEqual(b"unchanged", target.read_bytes())


if __name__ == "__main__":
    unittest.main()
