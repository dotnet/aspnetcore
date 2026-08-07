import tempfile
import unittest
from pathlib import Path

from validate_artifacts import REQUIRED_EXISTING, REQUIRED_NONEMPTY, validate


VALID_REVIEW = """# Multi-Model Review

**Orchestrator:** gpt-test

## Current fix
Current behavior.

## Independent candidates
Candidates.

## Adversarial consensus
Consensus.

## Test assessment
Assessment.

## Proof status
**Finding proof:** empirical
**Scenario proof:** empirical
**Candidate proof:** targeted-proven
**Product oracle:** documented
**Oracle fidelity:** authoritative
**Mechanism fidelity:** reproduced
**Scenario fidelity:** proxy
**Assertion disposition:** diagnostic-only

## Final recommendation
**Implementation verdict:** REVISE
**Behavioral evidence:** empirical
**Merge readiness:** recommendation only
**Implementation confidence:** medium
**Reason:** The evidence supports a scoped recommendation.

## Required follow-ups
None.

## Repository oracle gaps
None.

## Suggested review comments
None.
"""


class ValidateArtifactsTests(unittest.TestCase):
    def create_root(self) -> Path:
        temp_dir = tempfile.TemporaryDirectory()
        self.addCleanup(temp_dir.cleanup)
        root = Path(temp_dir.name)

        for relative_path in REQUIRED_NONEMPTY:
            path = root / relative_path
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("evidence\n", encoding="utf-8")

        for relative_path in REQUIRED_EXISTING:
            path = root / relative_path
            path.parent.mkdir(parents=True, exist_ok=True)
            path.touch()

        (root / "final/review.md").write_text(VALID_REVIEW, encoding="utf-8")
        return root

    def test_valid_artifacts_pass(self):
        self.assertEqual([], validate(self.create_root()))

    def test_missing_required_artifact_fails(self):
        root = self.create_root()
        (root / "evidence/product-oracle.md").unlink()

        errors = validate(root)

        self.assertIn(
            "missing required artifact: evidence/product-oracle.md",
            errors,
        )

    def test_missing_final_recommendation_field_fails(self):
        root = self.create_root()
        review_path = root / "final/review.md"
        review_path.write_text(
            VALID_REVIEW.replace(
                "**Behavioral evidence:** empirical\n",
                "",
            ),
            encoding="utf-8",
        )

        errors = validate(root)

        self.assertIn(
            "final review missing marker: **Behavioral evidence:**",
            errors,
        )

    def test_weak_oracle_cannot_block_on_implementation(self):
        root = self.create_root()
        review_path = root / "final/review.md"
        review = VALID_REVIEW.replace(
            "**Oracle fidelity:** authoritative",
            "**Oracle fidelity:** hypothesis",
        ).replace(
            "**Merge readiness:** recommendation only",
            "**Merge readiness:** blocked on implementation",
        )
        review_path.write_text(review, encoding="utf-8")

        errors = validate(root)

        self.assertIn(
            "blocked on implementation requires stronger oracle, mechanism, and "
            "scenario fidelity",
            errors,
        )

    def test_inferred_mechanism_cannot_block_on_implementation(self):
        root = self.create_root()
        review_path = root / "final/review.md"
        review = VALID_REVIEW.replace(
            "**Mechanism fidelity:** reproduced",
            "**Mechanism fidelity:** inferred",
        ).replace(
            "**Merge readiness:** recommendation only",
            "**Merge readiness:** blocked on implementation",
        )
        review_path.write_text(review, encoding="utf-8")

        errors = validate(root)

        self.assertIn(
            "blocked on implementation requires stronger oracle, mechanism, and "
            "scenario fidelity",
            errors,
        )

    def test_production_proven_requires_multiple_stress_cases(self):
        root = self.create_root()
        review_path = root / "final/review.md"
        review_path.write_text(
            VALID_REVIEW.replace(
                "**Candidate proof:** targeted-proven",
                "**Candidate proof:** production-proven",
            ),
            encoding="utf-8",
        )
        (root / "empirical/stress-matrix.md").write_text(
            "| Configuration | Result |\n|---|---|\n| only | pass |\n",
            encoding="utf-8",
        )

        errors = validate(root)

        self.assertIn(
            "production-proven requires a stress matrix with multiple executed cases",
            errors,
        )

    def test_production_proven_requires_explicit_coverage_dimensions(self):
        root = self.create_root()
        review_path = root / "final/review.md"
        review_path.write_text(
            VALID_REVIEW.replace(
                "**Candidate proof:** targeted-proven",
                "**Candidate proof:** production-proven",
            ).replace(
                "**Assertion disposition:** diagnostic-only",
                "**Assertion disposition:** merge-candidate",
            ),
            encoding="utf-8",
        )
        (root / "empirical/stress-matrix.md").write_text(
            "| Configuration | Result |\n"
            "|---|---|\n"
            "| Debug | pass |\n"
            "| Release | pass |\n",
            encoding="utf-8",
        )

        errors = validate(root)

        self.assertIn(
            "production-proven requires an explicit passed or justified "
            "not-applicable status for: Real producer/runtime boundary",
            errors,
        )

    def test_production_proven_accepts_complete_stress_evidence(self):
        root = self.create_root()
        review_path = root / "final/review.md"
        review_path.write_text(
            VALID_REVIEW.replace(
                "**Candidate proof:** targeted-proven",
                "**Candidate proof:** production-proven",
            ).replace(
                "**Assertion disposition:** diagnostic-only",
                "**Assertion disposition:** merge-candidate",
            ),
            encoding="utf-8",
        )
        (root / "empirical/stress-matrix.md").write_text(
            "**Real producer/runtime boundary:** passed in integration test\n"
            "**Varied falsification dimensions:** passed across exit paths\n"
            "**Applicable configurations/platforms:** passed on exact CI matrix\n"
            "**Neighboring suite:** passed\n"
            "**Cleanup/interruption paths:** not applicable - synchronous API\n\n"
            "| Configuration | Result |\n"
            "|---|---|\n"
            "| Debug | pass |\n"
            "| Release | pass |\n",
            encoding="utf-8",
        )

        self.assertEqual([], validate(root))

    def test_diagnostic_only_cannot_claim_high_confidence(self):
        root = self.create_root()
        review_path = root / "final/review.md"
        review = VALID_REVIEW.replace(
            "**Candidate proof:** targeted-proven",
            "**Candidate proof:** diagnostic-only",
        ).replace(
            "**Implementation confidence:** medium",
            "**Implementation confidence:** high",
        )
        review_path.write_text(review, encoding="utf-8")

        errors = validate(root)

        self.assertIn(
            "diagnostic-only candidate proof is incompatible with high confidence",
            errors,
        )


if __name__ == "__main__":
    unittest.main()
