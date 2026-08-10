import tempfile
import unittest
from pathlib import Path

from validate_evals import (
    file_sha256,
    held_out_hash,
    read_documents,
    summary,
    validate_documents,
)


def valid_eval() -> dict:
    return {
        "id": 1,
        "prompt": "Review a component behavior without using its internal name.",
        "files": ["fixture.md"],
        "expectations": [
            "The review establishes the observable behavior.",
            "The review rejects a stale-element failure as evidence.",
        ],
        "eval_metadata": {
            "mechanism": "lifecycle-retention",
            "provenance": {"kind": "historical", "source": "issue-123"},
            "area": "Components",
            "score_family": "lifecycle",
            "tier": "train",
            "discovery_mode": "discovery",
            "controls": {"positive": [0], "negative": [1]},
            "forbidden_prompt_terms": ["stale-element"],
        },
    }


class ValidateEvalsTests(unittest.TestCase):
    def test_valid_metadata_passes_and_reports_family_weight(self):
        result = validate_documents([("valid.json", {"evals": [valid_eval()]})])

        self.assertEqual([], result.errors)
        self.assertEqual(1, summary(result)["raw_count"])
        self.assertEqual(1.0, summary(result)["family_weights"][0]["weight"])

    def test_missing_metadata_fields_fail(self):
        eval_data = valid_eval()
        del eval_data["eval_metadata"]["area"]
        del eval_data["eval_metadata"]["provenance"]["source"]

        result = validate_documents([("invalid.json", {"evals": [eval_data]})])

        self.assertIn(
            "invalid.json: evals[0].eval_metadata.area must be a nonempty string",
            result.errors,
        )
        self.assertIn(
            "invalid.json: evals[0].eval_metadata.provenance.source must be a nonempty string",
            result.errors,
        )

    def test_missing_id_fails(self):
        eval_data = valid_eval()
        del eval_data["id"]

        result = validate_documents([("invalid.json", {"evals": [eval_data]})])

        self.assertIn(
            "invalid.json: evals[0].id must be a positive integer",
            result.errors,
        )

    def test_invalid_controls_fail(self):
        eval_data = valid_eval()
        eval_data["eval_metadata"]["controls"] = {
            "positive": [0, 3],
            "negative": [0],
        }

        result = validate_documents([("invalid.json", {"evals": [eval_data]})])

        self.assertIn(
            "invalid.json: evals[0].eval_metadata.controls.positive index 3 must reference expectations",
            result.errors,
        )
        self.assertIn(
            "invalid.json: evals[0].eval_metadata.controls positive and negative must be disjoint",
            result.errors,
        )

    def test_forbidden_prompt_term_fails_case_insensitively(self):
        eval_data = valid_eval()
        eval_data["eval_metadata"]["forbidden_prompt_terms"] = ["COMPONENT"]

        result = validate_documents([("leaked.json", {"evals": [eval_data]})])

        self.assertIn(
            "leaked.json: evals[0].eval_metadata.forbidden_prompt_terms contains prompt term: 'COMPONENT'",
            result.errors,
        )

    def test_discovery_prompt_rejects_pull_request_identity(self):
        prompts = [
            "Review PR #123.",
            "Review issue #68114.",
            "Review #68037.",
            "Review head 2df89be.",
        ]
        for prompt in prompts:
            with self.subTest(prompt=prompt):
                eval_data = valid_eval()
                eval_data["prompt"] = prompt

                result = validate_documents(
                    [("leaked.json", {"evals": [eval_data]})]
                )

                self.assertIn(
                    "leaked.json: evals[0].prompt must not expose issue, "
                    "pull request, or commit identities in discovery mode",
                    result.errors,
                )

    def test_concentration_is_warning_only(self):
        first = valid_eval()
        second = valid_eval()
        second["id"] = 2

        result = validate_documents([("concentrated.json", {"evals": [first, second]})])

        self.assertEqual([], result.errors)
        self.assertTrue(
            any("family concentration" in warning for warning in result.warnings)
        )
        self.assertTrue(
            any("provenance concentration" in warning for warning in result.warnings)
        )

    def test_family_weights_are_scoped_to_each_eval_file(self):
        first = valid_eval()
        second = valid_eval()
        second["id"] = 2

        result = validate_documents(
            [
                ("review-evals.json", {"evals": [first]}),
                ("try-fix-evals.json", {"evals": [second]}),
            ]
        )
        weights = summary(result)["family_weights"]

        self.assertEqual([1.0, 1.0], [weight["weight"] for weight in weights])

    def test_family_weights_are_normalized_within_each_tier(self):
        first = valid_eval()
        second = valid_eval()
        second["id"] = 2
        second["eval_metadata"]["score_family"] = "oracle"

        result = validate_documents([("evals.json", {"evals": [first, second]})])
        weights = summary(result)["family_weights"]

        self.assertEqual(1.0, sum(weight["weight"] for weight in weights))
        self.assertEqual([0.5, 0.5], [weight["weight"] for weight in weights])

    def test_held_out_hash_rejects_mutation(self):
        with tempfile.TemporaryDirectory() as directory:
            fixture = Path(directory) / "fixture.md"
            fixture.write_text("fixture", encoding="utf-8")
            eval_data = valid_eval()
            eval_data["files"] = [str(fixture)]
            eval_data["eval_metadata"]["tier"] = "held_out"
            eval_data["eval_metadata"]["fixture_hashes"] = {
                str(fixture): file_sha256(fixture)
            }
            eval_data["eval_metadata"]["frozen_hash"] = held_out_hash(eval_data)
            eval_data["prompt"] = "Mutated after freezing."

            result = validate_documents(
                [(str(Path(directory) / "evals.json"), {"evals": [eval_data]})]
            )

        self.assertIn(
            f"{Path(directory) / 'evals.json'}: "
            "evals[0].eval_metadata.frozen_hash does not match the held-out eval",
            result.errors,
        )

    def test_held_out_fixture_hash_rejects_mutation(self):
        with tempfile.TemporaryDirectory() as directory:
            fixture = Path(directory) / "fixture.md"
            fixture.write_text("fixture", encoding="utf-8")
            eval_data = valid_eval()
            eval_data["files"] = [str(fixture)]
            eval_data["eval_metadata"]["tier"] = "held_out"
            eval_data["eval_metadata"]["fixture_hashes"] = {
                str(fixture): file_sha256(fixture)
            }
            eval_data["eval_metadata"]["frozen_hash"] = held_out_hash(eval_data)
            fixture.write_text("mutated fixture", encoding="utf-8")

            result = validate_documents(
                [(str(Path(directory) / "evals.json"), {"evals": [eval_data]})]
            )

        self.assertTrue(
            any("does not match the fixture" in error for error in result.errors)
        )

    def test_duplicate_eval_ids_fail(self):
        first = valid_eval()
        second = valid_eval()

        result = validate_documents(
            [("duplicates.json", {"evals": [first, second]})]
        )

        self.assertIn(
            "duplicates.json.evals contains duplicate ids: 1",
            result.errors,
        )

    def test_checked_in_eval_suites_pass(self):
        skill_root = Path(__file__).resolve().parents[1]
        eval_paths = [
            skill_root / "evals" / "evals.json",
            skill_root.parent / "aspnetcore-try-fix" / "evals" / "evals.json",
        ]
        documents, read_errors = read_documents(eval_paths)

        self.assertEqual([], read_errors)
        self.assertEqual([], validate_documents(documents).errors)


if __name__ == "__main__":
    unittest.main()
