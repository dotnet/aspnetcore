import unittest

from aggregate_eval_scores import aggregate_document


class AggregateEvalScoresTests(unittest.TestCase):
    def test_family_macro_does_not_reward_duplicate_family_cases(self):
        document = {
            "evals": [
                self.eval_data(1, "train", "family-a", "source-a"),
                self.eval_data(2, "train", "family-a", "source-a"),
                self.eval_data(3, "train", "family-b", "source-b"),
                self.eval_data(4, "held_out", "family-c", "source-c"),
            ]
        }
        result, errors = aggregate_document(
            document,
            {"1": 1.0, "2": 1.0, "3": 0.0, "4": 0.5},
        )

        self.assertEqual([], errors)
        self.assertAlmostEqual(0.5, result["tiers"]["train"]["family_macro"])
        self.assertAlmostEqual(0.5, result["tiers"]["train"]["provenance_macro"])
        self.assertAlmostEqual(2 / 3, result["tiers"]["train"]["raw_mean"])

    def test_scores_must_cover_exact_eval_ids(self):
        document = {
            "evals": [
                self.eval_data(1, "train", "family-a", "source-a"),
            ]
        }

        result, errors = aggregate_document(
            document,
            {"2": 1.0},
        )

        self.assertEqual({}, result)
        self.assertIn("missing eval scores: 1", errors)
        self.assertIn("unknown eval scores: 2", errors)

    def test_transfer_gap_is_null_without_both_tiers(self):
        document = {
            "evals": [
                self.eval_data(1, "train", "family-a", "source-a"),
            ]
        }

        result, errors = aggregate_document(document, {"1": 1.0})

        self.assertEqual([], errors)
        self.assertIsNone(result["transfer_gap"]["family_macro"])
        self.assertIsNone(result["transfer_gap"]["provenance_macro"])

    @staticmethod
    def eval_data(
        identifier: int,
        tier: str,
        family: str,
        provenance: str,
    ) -> dict:
        return {
            "id": identifier,
            "eval_metadata": {
                "tier": tier,
                "score_family": family,
                "provenance": {
                    "kind": "synthetic",
                    "source": provenance,
                },
            },
        }


if __name__ == "__main__":
    unittest.main()
