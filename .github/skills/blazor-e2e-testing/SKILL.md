---
name: blazor-e2e-testing
description: >-
  Run a bounded selection of Blazor end-to-end tests in dotnet/aspnetcore. USE FOR validating a focused Components change with one test or class, and validating a major Blazor change by splitting affected Selenium E2E tests into small logical groups and running them sequentially. DO NOT USE FOR running the full Components E2E suite locally, repository setup or build troubleshooting, temporary sample validation (use validate-blazor-feature), unit tests, or non-Components areas.
---

# Run bounded Blazor E2E test groups

For permanent Selenium E2E coverage, follow [Bounded local E2E validation](../../../src/Components/AGENTS.md#bounded-local-e2e-validation). That section is the source of truth for focused method and class runs, logical groups for major changes, execution order, failure handling, and completion criteria.

For temporary sample and browser validation before permanent coverage, use the [`validate-blazor-feature`](../validate-blazor-feature/SKILL.md) skill instead.
