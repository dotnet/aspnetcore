---
name: create-kbe
description: Select and verify a Build Insights Known Build Error matcher for one concrete ASP.NET Core CI test failure.
---

# Create a Known Build Error for ASP.NET Core

Use this skill only after the caller has selected one exact failing test from
the workflow's deterministic Case A eligible-test array.

Read and follow these in order:

1. `.github/workflows/shared/create-kbe.instructions.md`
2. The caller workflow's quarantine eligibility and output instructions

Core rules:

- One exact test and one failure shape per outcome.
- Search existing open and recently closed Known Build Errors before proposing
  a new matcher.
- Prefer a literal message, then an ordered literal array. Use a bounded regex
  only when literals cannot identify the failure safely.
- Never use a bare test name, stack frame, exception type, assertion stem,
  timeout, exit code, or other broad category.
- Copy matcher values exactly from the supplied failure evidence.
- If the matcher, evidence, or duplicate search is uncertain, report the KBE as
  incomplete. The quarantine issue may still be created without activating
  Build Insights matching.
- Never promote a test absent from the deterministic eligible-test array. The
  agent cannot replace or override the collector-authored eligibility receipt.
- Do not render the final issue body or choose labels. The deterministic
  quarantine issue handler owns those operations.
