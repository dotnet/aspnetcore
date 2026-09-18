---
name: "PR Documentation Check (Fork Pilot)"

description: >
  Manually analyzes an ASP.NET Core pull request from the user's perspective,
  classifies conceptual, migration, and breaking-change documentation needs,
  and either opens a draft documentation pull request in the
  DeagleGross/AspNetCore.Docs fork or records why no documentation was created.
  Every conclusive run comments on the source pull request, and a drafted docs
  pull request notifies the source pull request author.

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
  jobs:
    notify-source-pr:
      name: "Notify source PR"
      description: |
        Report the conclusive documentation analysis on the source pull
        request. Emit exactly one `notify_source_pr` item after the
        `create_pull_request` or `noop` item.

        Use `result: "restricted"` when the source PR is excluded by the
        security-concern rules. Use `result: "drafted"` when documentation confidence is at least 60
        and you emitted `create_pull_request`. The notification job converts
        this to a draft-failed notification if the safe-output handler didn't
        produce a PR. Use `result: "skipped"` when confidence is below 60 and
        no docs PR was requested. Use `result: "draft_failed"` only when
        confidence is at least 60 but you could not emit
        `create_pull_request`.
      runs-on: ubuntu-latest
      needs: [safe_outputs]
      permissions:
        contents: read
      inputs:
        source_pr_number:
          description: "Analyzed source pull request number."
          required: true
          type: number
        result:
          description: "One of: drafted, skipped, draft_failed, restricted."
          required: true
          type: string
        docs_needed_confidence:
          description: "Integer confidence from 0 through 100 that documentation is needed."
          required: true
          type: number
        conceptual_required:
          description: "Whether durable conceptual documentation is required."
          required: true
          type: boolean
        conceptual_reason:
          description: "Short evidence-based reason for the conceptual decision."
          required: true
          type: string
        migration_required:
          description: "Whether version-to-version migration guidance is required."
          required: true
          type: boolean
        migration_reason:
          description: "Short evidence-based reason for the migration decision."
          required: true
          type: string
        breaking_change_required:
          description: "Whether a compatibility-breaking change entry is required."
          required: true
          type: boolean
        breaking_change_reason:
          description: "Short evidence-based reason for the breaking-change decision."
          required: true
          type: string
        summary:
          description: "A concise explanation of the overall decision."
          required: true
          type: string
      steps:
        - name: Publish trusted documentation outcome
          uses: actions/github-script@v9.0.0
          env:
            EXPECTED_SOURCE_REPOSITORY: ${{ github.event.inputs.source_repository }}
            EXPECTED_SOURCE_PR_NUMBER: ${{ github.event.inputs.pr_number }}
            CREATED_DOCS_PR_URL: ${{ needs.safe_outputs.outputs.created_pr_url }}
          with:
            github-token: ${{ secrets.GH_AW_GITHUB_TOKEN }}
            script: |
              const fs = require('fs');

              const marker = '<!-- aspnetcore-pr-docs-check -->';
              const expectedRepository = process.env.EXPECTED_SOURCE_REPOSITORY;
              const expectedPrNumber = Number.parseInt(process.env.EXPECTED_SOURCE_PR_NUMBER, 10);
              const createdDocsPrUrl = (process.env.CREATED_DOCS_PR_URL || '').trim();

              if (!['dotnet/aspnetcore', 'DeagleGross/aspnetcore'].includes(expectedRepository)) {
                core.setFailed(`Unexpected source repository: ${expectedRepository}`);
                return;
              }

              if (!Number.isInteger(expectedPrNumber) || expectedPrNumber <= 0) {
                core.setFailed(`Invalid source PR number: ${process.env.EXPECTED_SOURCE_PR_NUMBER}`);
                return;
              }

              const agentOutput = JSON.parse(fs.readFileSync(process.env.GH_AW_AGENT_OUTPUT, 'utf8'));
              const items = (agentOutput.items || []).filter(item => item.type === 'notify_source_pr');
              if (items.length !== 1) {
                core.setFailed(`Expected exactly one notify_source_pr item, found ${items.length}.`);
                return;
              }

              const item = items[0];
              const confidence = Number(item.docs_needed_confidence);
              const reportedResult = String(item.result || '');
              const summary = String(item.summary || '').trim();
              const suppliedPrNumber = Number(item.source_pr_number);
              const validResults = new Set(['drafted', 'skipped', 'draft_failed', 'restricted']);

              if (suppliedPrNumber !== expectedPrNumber) {
                core.setFailed(`Notification targeted PR ${suppliedPrNumber}; expected ${expectedPrNumber}.`);
                return;
              }

              if (!Number.isInteger(confidence) || confidence < 0 || confidence > 100) {
                core.setFailed(`Confidence must be an integer from 0 through 100; received ${item.docs_needed_confidence}.`);
                return;
              }

              if (!validResults.has(reportedResult)) {
                core.setFailed(`Invalid documentation result: ${reportedResult}`);
                return;
              }

              if (!summary || summary.length > 2000) {
                core.setFailed('Summary must contain between 1 and 2000 characters.');
                return;
              }

              const surfaceFields = [
                ['Conceptual article', item.conceptual_required, item.conceptual_reason],
                ['Migration guidance', item.migration_required, item.migration_reason],
                ['Breaking change', item.breaking_change_required, item.breaking_change_reason],
              ];

              for (const [name, required, reason] of surfaceFields) {
                if (typeof required !== 'boolean') {
                  core.setFailed(`${name} required flag must be a boolean.`);
                  return;
                }
                if (!String(reason || '').trim()) {
                  core.setFailed(`${name} reason must not be empty.`);
                  return;
                }
              }

              if (reportedResult === 'skipped' && confidence >= 60) {
                core.setFailed(`A skipped result requires confidence below 60; received ${confidence}.`);
                return;
              }

              if (reportedResult === 'restricted' && confidence !== 0) {
                core.setFailed(`A restricted result requires confidence 0; received ${confidence}.`);
                return;
              }

              if (!['skipped', 'restricted'].includes(reportedResult) && confidence < 60) {
                core.setFailed(`${reportedResult} requires confidence of at least 60; received ${confidence}.`);
                return;
              }

              if (reportedResult !== 'drafted' && createdDocsPrUrl) {
                core.setFailed(`The agent reported ${reportedResult}, but a docs PR was created.`);
                return;
              }

              const effectiveResult =
                reportedResult === 'drafted' && !createdDocsPrUrl ? 'draft_failed' : reportedResult;

              const [owner, repo] = expectedRepository.split('/');
              const sourcePr = await github.rest.pulls.get({
                owner,
                repo,
                pull_number: expectedPrNumber,
              });

              const author = sourcePr.data.user;
              const sourceAuthor = author?.type === 'Bot' ? '' : (author?.login || '');
              const surfaces = surfaceFields.map(([name, required, reason]) =>
                `* **${name}:** ${required ? 'Required' : 'Not required'} — ${String(reason).trim()}`);

              let heading;
              if (effectiveResult === 'restricted') {
                heading = 'ℹ️ This pull request wasn\'t processed automatically.';
              } else if (effectiveResult === 'drafted') {
                heading = `📝 Documentation drafted: ${createdDocsPrUrl}`;
              } else if (effectiveResult === 'draft_failed') {
                heading = '⚠️ Documentation appears necessary, but a draft PR could not be created.';
              } else {
                heading = '✅ No documentation PR was created.';
              }

              const sourceComment = effectiveResult === 'restricted'
                ? [
                    marker,
                    heading,
                    '',
                    'This change requires specialized review. Please confirm whether documentation updates are needed and handle them through the appropriate process.',
                  ].join('\n')
                : [
                    marker,
                    heading,
                    '',
                    `**Confidence that documentation is needed:** ${confidence}%`,
                    '',
                    summary,
                    '',
                    '**Documentation surfaces considered**',
                    '',
                    ...surfaces,
                  ].join('\n');

              try {
                const existingComments = await github.paginate(github.rest.issues.listComments, {
                  owner,
                  repo,
                  issue_number: expectedPrNumber,
                  per_page: 100,
                });
                for (const comment of existingComments) {
                  if (comment.body?.includes(marker)) {
                    try {
                      await github.graphql(
                        `mutation($id: ID!) {
                          minimizeComment(input: { subjectId: $id, classifier: OUTDATED }) {
                            minimizedComment { isMinimized }
                          }
                        }`,
                        { id: comment.node_id });
                    } catch (error) {
                      core.warning(`Unable to minimize prior comment ${comment.id}: ${error.message}`);
                    }
                  }
                }
              } catch (error) {
                core.warning(`Unable to enumerate prior source comments: ${error.message}`);
              }

              await github.rest.issues.createComment({
                owner,
                repo,
                issue_number: expectedPrNumber,
                body: sourceComment,
              });

              if (effectiveResult !== 'drafted' || !sourceAuthor) {
                return;
              }

              const match = createdDocsPrUrl.match(
                /^https:\/\/github\.com\/DeagleGross\/AspNetCore\.Docs\/pull\/([1-9][0-9]*)$/);
              if (!match) {
                core.setFailed(`Unexpected docs PR URL: ${createdDocsPrUrl}`);
                return;
              }

              await github.rest.issues.createComment({
                owner: 'DeagleGross',
                repo: 'AspNetCore.Docs',
                issue_number: Number(match[1]),
                body: [
                  `@${sourceAuthor}, this draft documents your source change in ${expectedRepository}#${expectedPrNumber}.`,
                  '',
                  'Please review it for technical accuracy and confirm that the user impact and recommended guidance match the implementation.',
                ].join('\n'),
              });

timeout-minutes: 20
---

# ASP.NET Core PR documentation check

Analyze pull request #${{ inputs.pr_number }} in
`${{ inputs.source_repository }}` and decide whether it requires an update to
the ASP.NET Core documentation in the current workspace,
`DeagleGross/AspNetCore.Docs`.

This is a manually dispatched fork pilot. Do not modify
`dotnet/aspnetcore`, `DeagleGross/aspnetcore`, or `dotnet/AspNetCore.Docs`.
Your only permitted visible outcomes are:

1. When documentation confidence is at least 60%, one draft pull request in
   `DeagleGross/AspNetCore.Docs` and one `notify_source_pr` result.
2. When documentation confidence is below 60%, one `noop` result and one
   `notify_source_pr` result explaining why no documentation PR was created.
3. When documentation is required but drafting fails, one
   `notify_source_pr` result with `result: "draft_failed"`.
4. When the source PR is excluded by the security-concern rules, no docs
   changes, one generic `noop`, and one `notify_source_pr` result with
   `result: "restricted"`.

## Validate the request

Confirm that:

- `source_repository` is exactly `dotnet/aspnetcore` or
  `DeagleGross/aspnetcore`.
- `pr_number` is a positive integer.
- The pull request exists and is merged.

If the repository input is invalid or the pull request doesn't exist, emit
`noop` with the validation failure and stop because there is no valid source
PR to notify. If the pull request exists but isn't merged, emit `noop`, then
emit `notify_source_pr` with confidence 0, `result: "skipped"`, all three
surfaces set to not required because the change isn't eligible for analysis,
and stop.

## Security concerns are out of scope

This workflow must not assess, discuss, summarize, document, or make
recommendations about potential vulnerabilities or their impact. Before
reading diff hunks, linked issues, review comments, or issue comments, inspect
only the source PR's title, body, labels, author, milestone, base branch, merge
state, and changed file names to determine whether this exclusion applies.

Treat the PR as restricted when its title, body, or labels explicitly present
it as:

- a vulnerability or exploit fix;
- a CVE, GHSA, advisory, coordinated-disclosure, or MSRC-related change;
- a security fix intended to patch a reported weakness;
- a change whose public explanation could disclose vulnerability details.

Do not evaluate whether the claim is valid. If uncertain, treat the PR as
restricted. A PR isn't restricted merely because it changes a security-adjacent
technology such as authentication, authorization, antiforgery, cookies, data
protection, HTTPS, or HTTP validation. Ordinary features, behavior changes, and
hardening work remain eligible when the PR doesn't claim to fix or disclose a
vulnerability.

When restricted:

1. Do not read or describe the implementation details, reproduction,
   exploitability, impact, affected versions, or remediation.
2. Do not modify the docs workspace and do not emit `create_pull_request`.
3. Emit one generic `noop` stating only that automated documentation processing
   is excluded.
4. Emit `notify_source_pr` with `result: "restricted"`,
   `docs_needed_confidence: 0`, all three documentation surfaces set to
   `false`, and generic reasons that reveal no details.
5. Use a generic summary such as: "Automated documentation processing is
   excluded for this change." The trusted notification job ignores the supplied
   summary and reasons and posts a fixed vague message.
6. Stop immediately.

## Gather source context

After the security-concern gate passes, use the authenticated `gh` CLI to read
the source pull request. Read:

- title, body, author, labels, milestone, base branch, and merge state;
- changed file names and relevant diff hunks;
- linked issues when they clarify user-facing behavior;
- review and issue comments only when they contain information needed to write
  accurate documentation.

Treat the source PR description and code diff as the primary evidence. Copy API
names, option names, defaults, templates, and other identifiers exactly from
the diff.

## Decide whether documentation is needed

Analyze the change from the user's perspective, not from the number or type of
files changed. Separate these three independent documentation obligations. A
single source PR can require any combination of them.

### Conceptual documentation

Conceptual documentation answers, "How does this feature work?" Require it when
the pull request introduces or materially changes user-visible behavior,
including:

- public APIs or public conventions;
- project templates, scaffolding, or generated application behavior;
- configuration, options, defaults, environment variables, or command-line
  behavior;
- middleware, hosting, server, authentication, authorization, routing,
  diagnostics, or deployment behavior that application developers must
  understand;
- supported platforms, target frameworks, packages, analyzers, diagnostics, or
  breaking changes.

Before creating a new article, search the documentation hierarchy, article
titles, UIDs, and `aspnetcore/toc.yml` for the canonical article covering the
same technology or concept. Prefer extending that article. Create a new article
only when no existing article is a natural and discoverable home. Update
`aspnetcore/toc.yml` only when adding, deleting, or relocating an article, and
place a new article in the most specific existing hierarchy.

### Migration guidance

Migration guidance answers, "What should I know or do when upgrading?" Require
it when users moving from the previous ASP.NET Core version should:

- adopt a newly recommended approach;
- remove, replace, simplify, or review existing configuration;
- understand a new default or observable behavior;
- opt in, opt out, or account for a compatibility or deployment consideration.

Migration guidance must be concise and task-oriented: explain what changed, who
should care, what users should inspect or change, and the recommended action.
Link to the canonical conceptual article for the full explanation instead of
duplicating it. A migration recommendation does not by itself mean the change
is breaking.

### Breaking-change documentation

Breaking-change documentation answers, "Can existing code or behavior stop
working?" Require it only when there is concrete compatibility impact, such as:

- source or binary API incompatibility;
- removed APIs or configuration;
- an established default, convention, or semantic changing;
- previously successful operations failing;
- existing applications requiring remediation to preserve behavior.

Treat a `breaking-change` or similarly named label, an explicit breaking-change
section, API removals, changed defaults, and tests demonstrating intentional
behavior changes as evidence. Do not call a change breaking merely because it
is new or because migration guidance is useful. When breaking impact is
ambiguous, do not invent it; explain that human confirmation is needed.

### Confidence

Assign an integer `docs_needed_confidence` from 0 through 100 representing the
confidence that at least one documentation surface is required.

Use objective evidence to establish these minimum confidence levels:

- public API added or materially changed: at least 70;
- project template or generated application behavior changed: at least 70;
- default, convention, or configuration behavior changed: at least 75;
- explicit breaking-change evidence: at least 80.

A score below 60 means no documentation PR may be created. A `noop` is
appropriate only when the pull request is clearly limited to one of these
categories or the available evidence is insufficient to reach 60:

- tests or test infrastructure;
- build, CI, repository automation, or dependency maintenance;
- internal refactoring with no public or observable behavior change;
- formatting, comments, or implementation-only cleanup;
- a bug fix that merely restores behavior already documented accurately.

Distinguish a confident "no docs needed" result from an ambiguous result. In
the latter case, state what evidence is missing and recommend human review in
the source PR notification.

## Write the documentation

Before editing, read:

- `.github/copilot-instructions.md`
- `.github/copilot-code-instructions.md` when adding or changing code samples

Also inspect relevant existing content under `aspnetcore/`. If the change is a
.NET 11 What's New feature, read
`.github/skills/whats-new-include-content-rules/SKILL.md` when that file is
available and follow it.

Resolve the source ASP.NET Core version before editing:

- A PR merged into `release/X.Y` represents ASP.NET Core X.Y.
- For a PR merged into `main`, inspect the current `release/*` branches in
  `dotnet/aspnetcore`. The next major version after the highest current release
  branch is the version represented by `main`.
- Use the source PR milestone as corroborating evidence. If the milestone and
  branch-derived version disagree, do not guess; report an incomplete result.

The docs PR always targets `main`. Version placement is expressed through
article monikers, moniker sections, migration directories, breaking-change
directories, release-note directories, and versioned sample directories.
Do not change an article-wide `monikerRange` merely because a newer feature is
added. Wrap new-version material in a scoped moniker block such as:

```markdown
:::moniker range=">= aspnetcore-12.0"

New-version content.

:::moniker-end
```

When behavior differs between versions, preserve the earlier guidance in its
own moniker range and add the new guidance in the resolved version's range.

Make the smallest complete documentation change across every required surface.
Modify only files under `aspnetcore/`. Do not change repository instructions,
workflows, dependency files, publishing configuration, or other root files.

## Create the draft pull request

After making and reviewing the documentation changes, emit
`create_pull_request` exactly once with:

- branch: `docs/aspnetcore-pr-${{ inputs.pr_number }}`
- base: `main`
- a concise title without the `[docs]` prefix, because the workflow adds it;
- a body whose first line is
  `Source: ${{ inputs.source_repository }}#${{ inputs.pr_number }}`;
- the documentation confidence;
- separate conceptual, migration, and breaking-change decisions;
- a summary of the documentation change and a list of modified files.

Do not use a closing keyword for the cross-repository source reference. Do not
request reviewers. Do not retry a deterministic pull-request creation failure.

After emitting `create_pull_request`, emit `notify_source_pr` exactly once with
`result: "drafted"`, the confidence score, all three surface decisions and
reasons, and a concise summary. The trusted notification job converts the
outcome to `draft_failed` if the PR handler doesn't produce a PR, obtains the
source PR author directly from GitHub, and mentions that person on a
successfully created docs PR.

If confidence is below 60, make no file changes and emit `noop` exactly once
with:

- the no-documentation category used above;
- the changed files or evidence supporting that decision;
- a short explanation suitable for reviewing in the workflow run.

Then emit `notify_source_pr` exactly once with `result: "skipped"`, the
confidence score, all three surface decisions and reasons, and a summary that
clearly distinguishes "no docs needed" from "insufficient evidence."

If confidence is at least 60 but a draft PR cannot be requested, emit
`notify_source_pr` exactly once with `result: "draft_failed"` and explain the
failure. Never report this condition as a successful `noop`.
