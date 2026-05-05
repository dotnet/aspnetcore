# Triage Process

This document describes how issues filed in `dotnet/aspnetcore` are triaged. It is intended as:

- A reference for the **.NET community** so you know what to expect after filing an issue.
- A guide for **engineers joining the team** on how to triage incoming issues consistently.

> **Need urgent help?** Customers needing urgent investigation should contact [Microsoft Support](https://dotnet.microsoft.com/platform/support). For security vulnerabilities, follow the [.NET security policy](https://github.com/dotnet/aspnetcore/security/policy) — please **do not** file a public issue.

## How we triage

The team reviews untriaged issues (open issues without a milestone) regularly. We try to respond to new issues as quickly as we can, but it may take a few days. Each triaged issue is given an **issue type**, an **`area-*` label**, and a **milestone** — together they answer "what kind of issue is this, who owns it, and when (if ever) are we planning to work on it?"

A milestone reflects current intent, not a guarantee. Issues placed in a planning milestone may be rescheduled, moved to `Backlog`, or closed as priorities evolve.

## Triage outcomes

A triaged issue ends up in one of these states:

| Outcome | Meaning |
| --- | --- |
| **Closed** | Not actionable: duplicate, by-design, won't fix, can't repro, or already fixed. |
| **Servicing milestone** (`N.0.x`) | A regression or high-impact bug in a supported release that warrants a patch. Subject to the [servicing bar](./Servicing.md). |
| **Current release milestone** (`N.0-previewX`) | Needs to be addressed in the next preview of the in-flight release — for example, a recent regression or a blocker for an in-progress feature. |
| **Current-release planning milestone** (`.NET N Planning`) | Candidate for the in-flight major release. Issues here are scheduled into a specific `N.0-previewX` milestone during team planning. |
| **`Backlog`** | Tracked but not committed to any release. Re-evaluated during release planning. |

If we need more information from the reporter before we can triage, we apply the `Needs: Author Feedback` or `Needs: Repro` label and triage again once the information is provided. See [Issue Management Policies](./IssueManagementPolicies.md) for how those labels behave.

## Issue types

Each triaged issue is assigned a [GitHub issue type](https://docs.github.com/en/issues/tracking-your-work-with-issues/configuring-a-custom-issue-type-list-for-an-organization): **Bug**, **Feature**, **Task**, or **Epic**. The decision logic differs by type.

### Bug

A good bug report describes:

1. The expected behavior.
2. The actual behavior.
3. The .NET version and OS.
4. A minimal reproduction. See the [Bug Report Reproduction Guide](./repro.md) for what we expect — typically a project hosted in a public GitHub repo. If a repro is missing, we apply `Needs: Repro`.

We assess impact and severity by considering:

- **Is it a regression in a currently supported release?** Regressions are weighted heavily and are the strongest candidates for servicing.
- **Severity:** crash, data loss, security, hang, perf, or incorrect behavior with a workaround?
- **Reach:** how many users are likely affected? Common scenario or a niche edge case?
- **Community signal:** 👍 reactions and substantive comments from distinct users.
- **Workaround availability.**

Outcomes:

- **Clear regression in a supported release with no good workaround** → servicing milestone, subject to the [servicing bar](./Servicing.md) (which also weighs regression risk and forbids API changes in patches).
- **Recent regression on `main` or a blocker for in-progress work** → current release milestone (`N.0-previewX`).
- **High impact, reproducible, but not blocking the next preview** → current-release planning milestone.
- **Real but low impact, or unclear impact** → `Backlog`.
- **Caused by user code or environment** → close with explanation.

### Feature

We try to prioritize the most important and impactful feature requests for each release. There are multiple factors that go into determining the priority of a feature. It's a balance between addressing key feedback, investing in key strategic directions, and satisfying business needs. We also have to weigh the cost and risk of building, maintaining, and supporting the feature given the team's available resources.

Outcomes:

- **Committed for the next preview** → current release milestone (`N.0-previewX`).
- **Aligned and prioritized for the current release** → current-release planning milestone.
- **Reasonable but not prioritized** → `Backlog` to gather signal.
- **Out of scope** → close with rationale and, where possible, a pointer to alternatives.

### Task

Engineering work that is neither a user-visible bug nor a new feature: refactors, perf improvements without a behavior change, test infrastructure, build/CI work, internal cleanups, and **documentation gaps**.

Documentation tasks in this repo track technical content gaps that need to be filled by the ASP.NET Core engineering team. The engineering team provides the technical content to add to the docs by opening an issue with the content in the [`dotnet/AspNetCore.Docs`](https://github.com/dotnet/AspNetCore.Docs) repo. Doc issues should have type **Task** and the `Docs` label.

### Epic

Larger bodies of work that span multiple issues or releases. Epics are usually created by the team to track a strategic investment and link the constituent Bug/Feature/Task issues; it's uncommon to assign Epic during inbound triage.

## Investigations

When further investigation from our team is needed to determine the nature and impact of a reported issue, we apply the `investigate` label. Once the investigation is completed, the issue is triaged normally. Issues that don't have clear impact or severity may get closed without being investigated.

## Wrong repo

If an issue belongs in another repo (e.g. `dotnet/runtime`, `dotnet/sdk`, `dotnet/AspNetCore.Docs`, `dotnet/yarp`), we transfer it during triage.

## Release planning

Near the end of a release cycle, the team reviews `Backlog` and promotes items aligned with the next release into the next planning milestone. Issues that have stayed in `Backlog` across multiple releases without gaining traction are typically closed during this review — long backlog age is itself a signal that an issue isn't impactful enough to act on. Reopening is welcome when new information or community demand emerges.

See [Release Planning](./ReleasePlanning.md) for details.

## References

- [Issue Management Policies](./IssueManagementPolicies.md) — automation and labels (`Needs: Author Feedback`, `Needs: Repro`, etc.).
- [Bug Report Reproduction Guide](./repro.md) — what we expect from a minimal repro.
- [Release Planning](./ReleasePlanning.md) — how releases are scoped.
- [Servicing](./Servicing.md) — Servicing bar and release process.
