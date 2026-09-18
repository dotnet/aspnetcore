---
name: "PR Documentation Check (Fork Pilot)"

description: >
  Manually analyzes an ASP.NET Core pull request and either records that no
  documentation is needed or opens a draft documentation pull request in the
  DeagleGross/AspNetCore.Docs fork.

on:
  workflow_dispatch:
    inputs:
      source_repository:
        description: "Repository containing the source pull request"
        required: true
        default: "dotnet/aspnetcore"
        type: choice
        options:
          - "dotnet/aspnetcore"
          - "DeagleGross/aspnetcore"
      pr_number:
        description: "Pull request number to analyze"
        required: true
        type: string
  roles: [admin, maintainer, write]
  reaction: none
  status-comment: false

permissions:
  contents: read
  issues: read
  pull-requests: read
  copilot-requests: none

concurrency:
  group: "pr-docs-check-fork-pilot-${{ inputs.source_repository }}-${{ inputs.pr_number }}"
  cancel-in-progress: false
  job-discriminator: ${{ github.run_id }}

engine:
  id: copilot
  env:
    COPILOT_GITHUB_TOKEN: ${{ secrets.COPILOT_GITHUB_TOKEN }}

checkout:
  - repository: DeagleGross/AspNetCore.Docs
    path: .
    github-token: ${{ secrets.GH_AW_GITHUB_TOKEN }}
    current: true

tools:
  github:
    mode: gh-proxy
    toolsets: [repos, issues, pull_requests]
    github-token: ${{ secrets.GITHUB_TOKEN }}
    min-integrity: merged
    allowed-repos:
      - deaglegross/aspnetcore
      - dotnet/aspnetcore
  bash: [cat, find, git, grep, head, jq, sed]

network:
  allowed:
    - defaults
    - github

safe-outputs:
  github-token: ${{ secrets.GH_AW_GITHUB_TOKEN }}
  report-failure-as-issue: false
  noop:
    report-as-issue: false
  create-pull-request:
    target-repo: "DeagleGross/AspNetCore.Docs"
    base-branch: main
    title-prefix: "[docs] "
    labels: [documentation]
    draft: true
    fallback-as-issue: false
    auto-close-issue: false
    allowed-files:
      - "aspnetcore/**"
    allowed-branches:
      - "docs/aspnetcore-pr-*"
    protected-files: blocked

timeout-minutes: 20
---

# ASP.NET Core PR documentation check

Analyze pull request #${{ inputs.pr_number }} in
`${{ inputs.source_repository }}` and decide whether it requires an update to
the ASP.NET Core documentation in the current workspace,
`DeagleGross/AspNetCore.Docs`.

This is a manually dispatched fork pilot. Do not write to `dotnet/aspnetcore`,
`dotnet/AspNetCore.Docs`, or the source pull request. Your only permitted
visible outcomes are:

1. One draft pull request in `DeagleGross/AspNetCore.Docs`.
2. One `noop` result explaining why documentation is not needed.

## Validate the request

Confirm that:

- `source_repository` is exactly `dotnet/aspnetcore` or
  `DeagleGross/aspnetcore`.
- `pr_number` is a positive integer.
- The pull request exists and is merged.

If validation fails, emit `noop` with the validation failure and stop.

## Gather source context

Use the authenticated `gh` CLI to read the source pull request. Read:

- title, body, author, labels, milestone, base branch, and merge state;
- changed file names and relevant diff hunks;
- linked issues when they clarify user-facing behavior;
- review and issue comments only when they contain information needed to write
  accurate documentation.

Treat the source PR description and code diff as the primary evidence. Copy API
names, option names, defaults, templates, and other identifiers exactly from
the diff.

## Decide whether documentation is needed

Create documentation when the pull request introduces or materially changes
user-visible behavior, including:

- public APIs or public conventions;
- project templates, scaffolding, or generated application behavior;
- configuration, options, defaults, environment variables, or command-line
  behavior;
- middleware, hosting, server, authentication, authorization, routing,
  diagnostics, or deployment behavior that application developers must
  understand;
- supported platforms, target frameworks, packages, analyzers, diagnostics, or
  breaking changes.

A `noop` is appropriate only when the pull request is clearly limited to one
of these categories:

- tests or test infrastructure;
- build, CI, repository automation, or dependency maintenance;
- internal refactoring with no public or observable behavior change;
- formatting, comments, or implementation-only cleanup;
- a bug fix that merely restores behavior already documented accurately.

When evidence is mixed, prefer a small draft documentation PR over `noop`.

## Write the documentation

Before editing, read:

- `.github/copilot-instructions.md`
- `.github/copilot-code-instructions.md` when adding or changing code samples

Also inspect relevant existing content under `aspnetcore/`. If the change is a
.NET 11 What's New feature, read
`.github/skills/whats-new-include-content-rules/SKILL.md` when that file is
available and follow it.

Make the smallest complete documentation change. Modify only files under
`aspnetcore/`. Do not change repository instructions, workflows, dependency
files, publishing configuration, or other root files.

## Create the draft pull request

After making and reviewing the documentation changes, emit
`create_pull_request` exactly once with:

- branch: `docs/aspnetcore-pr-${{ inputs.pr_number }}`
- base: `main`
- a concise title without the `[docs]` prefix, because the workflow adds it;
- a body whose first line is
  `Source: ${{ inputs.source_repository }}#${{ inputs.pr_number }}`;
- a summary of the documentation change and a list of modified files.

Do not use a closing keyword for the cross-repository source reference. Do not
request reviewers. Do not retry a deterministic pull-request creation failure.

If no documentation is needed, make no file changes and emit `noop` exactly
once with:

- the no-documentation category used above;
- the changed files or evidence supporting that decision;
- a short explanation suitable for reviewing in the workflow run.
