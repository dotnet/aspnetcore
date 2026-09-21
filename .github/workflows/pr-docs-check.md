---
name: "PR Documentation Check"

description: >
  Runs only when manually dispatched to analyze an ASP.NET Core pull request from the user's perspective, classify conceptual, migration, and breaking-change documentation needs, and either open a fork-backed draft documentation pull request in dotnet/AspNetCore.Docs or record why no documentation was created. Documentation branches are pushed to dotnet/AspNetCore.Docs.Automation. Every conclusive run comments on the source pull request, and a drafted docs pull request notifies the source pull request author.

max-turns: 100

on:
  # Keep the upstream rollout manual-only. Add an automatic merged-PR trigger separately after production approval.
  workflow_dispatch:
    inputs:
      source_repository:
        description: "Repository containing the source pull request"
        required: true
        default: "dotnet/aspnetcore"
        type: choice
        options:
          - "dotnet/aspnetcore"
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
  group: "pr-docs-check-${{ inputs.source_repository }}-${{ inputs.pr_number }}"
  cancel-in-progress: false
  job-discriminator: ${{ github.run_id }}

engine:
  id: copilot
  env:
    COPILOT_GITHUB_TOKEN: ${{ secrets.COPILOT_GITHUB_TOKEN }}

checkout:
  - repository: dotnet/AspNetCore.Docs
    path: .
    github-app:
      client-id: ${{ secrets.ASPNETCORE_DOCS_BOT_CLIENT_ID }}
      private-key: ${{ secrets.ASPNETCORE_DOCS_BOT_PRIVATE_KEY }}
      owner: dotnet
      repositories: ["AspNetCore.Docs"]
    fetch: ["*"]
    current: true

tools:
  github:
    mode: gh-proxy
    toolsets: [repos, issues, pull_requests]
    github-app:
      client-id: ${{ secrets.ASPNETCORE_DOCS_BOT_CLIENT_ID }}
      private-key: ${{ secrets.ASPNETCORE_DOCS_BOT_PRIVATE_KEY }}
      owner: dotnet
      repositories: ["aspnetcore", "AspNetCore.Docs"]
    min-integrity: merged
    allowed-repos:
      - dotnet/aspnetcore
  bash: [cat, find, git, grep, head, jq, sed]

network:
  allowed:
    - defaults
    - github

safe-outputs:
  github-app:
    client-id: ${{ secrets.ASPNETCORE_DOCS_BOT_CLIENT_ID }}
    private-key: ${{ secrets.ASPNETCORE_DOCS_BOT_PRIVATE_KEY }}
    owner: dotnet
    repositories: ["AspNetCore.Docs"]
  report-failure-as-issue: false
  noop:
    report-as-issue: false
  steps:
    - name: Download trusted draft context
      uses: actions/download-artifact@v8.0.1
      with:
        name: pr-docs-check-context-${{ github.run_attempt }}
        path: ${{ runner.temp }}/pr-docs-check-context
    - name: Check out safe-output preflight validator
      uses: actions/checkout@v7.0.1
      with:
        persist-credentials: false
        path: _safe-output-validator
        sparse-checkout: .github/workflows/pr-docs-check/validate_outcome.py
        sparse-checkout-cone-mode: false
    - name: Reject inconsistent docs mutations
      env:
        EXPECTED_SOURCE_REPOSITORY: ${{ github.event.inputs.source_repository }}
        EXPECTED_SOURCE_PR_NUMBER: ${{ github.event.inputs.pr_number }}
        GH_AW_AGENT_OUTPUT: ${{ steps.setup-agent-output-env.outputs.GH_AW_AGENT_OUTPUT }}
      run: >-
        python3 _safe-output-validator/.github/workflows/pr-docs-check/validate_outcome.py
        --preflight
        --agent-output "${GH_AW_AGENT_OUTPUT}"
        --source-repository "${EXPECTED_SOURCE_REPOSITORY}"
        --source-pr-number "${EXPECTED_SOURCE_PR_NUMBER}"
        --expected-existing-draft "${RUNNER_TEMP}/pr-docs-check-context/existing-draft.json"
  create-pull-request:
    target-repo: "dotnet/AspNetCore.Docs"
    head-repo: "dotnet/AspNetCore.Docs.Automation"
    allowed-repos:
      - "dotnet/AspNetCore.Docs"
      - "dotnet/AspNetCore.Docs.Automation"
    head-github-app:
      client-id: ${{ secrets.ASPNETCORE_DOCS_BOT_CLIENT_ID }}
      private-key: ${{ secrets.ASPNETCORE_DOCS_BOT_PRIVATE_KEY }}
      owner: dotnet
      repositories: ["AspNetCore.Docs.Automation"]
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
    preserve-branch-name: true
    protected-files: blocked
  push-to-pull-request-branch:
    target: "*"
    target-repo: "dotnet/AspNetCore.Docs"
    head-repo: "dotnet/AspNetCore.Docs.Automation"
    allowed-repos:
      - "dotnet/AspNetCore.Docs"
      - "dotnet/AspNetCore.Docs.Automation"
    head-github-app:
      client-id: ${{ secrets.ASPNETCORE_DOCS_BOT_CLIENT_ID }}
      private-key: ${{ secrets.ASPNETCORE_DOCS_BOT_PRIVATE_KEY }}
      owner: dotnet
      repositories: ["AspNetCore.Docs.Automation"]
    required-title-prefix: "[docs] "
    required-labels: [documentation]
    fallback-as-pull-request: false
    check-branch-protection: false
    allowed-files:
      - "aspnetcore/**"
    protected-files: blocked
  update-pull-request:
    target: "*"
    target-repo: "dotnet/AspNetCore.Docs"
    required-title-prefix: "[docs] "
    required-labels: [documentation]
  jobs:
    notify-source-pr:
      name: "Notify source PR"
      description: |
        Report the conclusive documentation analysis on the source pull request. Emit exactly one `notify_source_pr` item after the create, update, or no-op output.

        Use `result: "restricted"` when the source PR is excluded by the security-concern rules. Use `result: "drafted"` when documentation confidence is at least 60 and you either emitted `create_pull_request` or updated the one trusted existing draft. The notification job converts an unfulfilled creation request to a draft-failed notification. Use `result: "skipped"` when confidence is below 60 and no docs PR was requested. Use `result: "draft_failed"` only when confidence is at least 60 but you could not request a docs PR operation.
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
        docs_pr_action:
          description: "One of: created, updated, none."
          required: true
          type: string
        existing_docs_pr_number:
          description: "Existing docs PR number when docs_pr_action is updated."
          required: false
          type: number
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
        - name: Check out trusted outcome validator
          uses: actions/checkout@v7.0.1
          with:
            persist-credentials: false
            path: _validator
            sparse-checkout: .github/workflows/pr-docs-check/validate_outcome.py
            sparse-checkout-cone-mode: false
        - name: Download trusted draft context
          uses: actions/download-artifact@v8.0.1
          with:
            name: pr-docs-check-context-${{ github.run_attempt }}
            path: ${{ runner.temp }}/pr-docs-check-context
        - name: Mint ASP.NET Core docs bot token
          id: docs-bot-token
          uses: actions/create-github-app-token@v3.2.0
          with:
            client-id: ${{ secrets.ASPNETCORE_DOCS_BOT_CLIENT_ID }}
            private-key: ${{ secrets.ASPNETCORE_DOCS_BOT_PRIVATE_KEY }}
            owner: dotnet
            repositories: |
              aspnetcore
              AspNetCore.Docs
        - name: Read resulting docs pull request
          uses: actions/github-script@v9.0.0
          env:
            CREATED_DOCS_PR_URL: ${{ needs.safe_outputs.outputs.created_pr_url }}
            DOCS_PR_METADATA_PATH: ${{ runner.temp }}/pr-docs-check-docs-pr.json
          with:
            github-token: ${{ steps.docs-bot-token.outputs.token }}
            script: |
              const fs = require('fs');

              const output = JSON.parse(fs.readFileSync(process.env.GH_AW_AGENT_OUTPUT, 'utf8'));
              const notifications = (output.items || []).filter(item => item.type === 'notify_source_pr');
              const createdUrl = (process.env.CREATED_DOCS_PR_URL || '').trim();
              let number = 0;

              if (createdUrl) {
                const match = createdUrl.match(
                  /^https:\/\/github\.com\/dotnet\/AspNetCore\.Docs\/pull\/([1-9][0-9]*)$/);
                if (!match) {
                  core.setFailed(`Unexpected created docs PR URL: ${createdUrl}`);
                  return;
                }
                number = Number(match[1]);
              } else if (notifications.length === 1 && notifications[0].docs_pr_action === 'updated') {
                number = Number(notifications[0].existing_docs_pr_number);
                if (!Number.isInteger(number) || number <= 0) {
                  core.setFailed(`Invalid existing docs PR number: ${notifications[0].existing_docs_pr_number}`);
                  return;
                }
              }

              let metadata = {};
              if (number) {
                try {
                  const response = await github.rest.pulls.get({
                    owner: 'dotnet',
                    repo: 'AspNetCore.Docs',
                    pull_number: number,
                  });
                  metadata = response.data;
                } catch (error) {
                  core.warning(`Unable to read docs PR #${number}: ${error.message}`);
                }
              }

              fs.writeFileSync(process.env.DOCS_PR_METADATA_PATH, JSON.stringify(metadata));
        - name: Prepare trusted documentation outcome
          env:
            CREATED_DOCS_PR_URL: ${{ needs.safe_outputs.outputs.created_pr_url }}
            DOCS_PR_METADATA_PATH: ${{ runner.temp }}/pr-docs-check-docs-pr.json
            EXPECTED_SOURCE_REPOSITORY: ${{ github.event.inputs.source_repository }}
            EXPECTED_SOURCE_PR_NUMBER: ${{ github.event.inputs.pr_number }}
            DOCS_BOT_APP_SLUG: ${{ steps.docs-bot-token.outputs.app-slug }}
            SAFE_OUTPUTS_RESULT: ${{ needs.safe_outputs.result }}
            SAFE_OUTPUTS_ITEMS_FAILED: ${{ needs.safe_outputs.outputs.process_safe_outputs_items_failed }}
          run: >-
            python3 _validator/.github/workflows/pr-docs-check/validate_outcome.py
            --agent-output "${GH_AW_AGENT_OUTPUT}"
            --source-repository "${EXPECTED_SOURCE_REPOSITORY}"
            --source-pr-number "${EXPECTED_SOURCE_PR_NUMBER}"
            --created-pr-url "${CREATED_DOCS_PR_URL}"
            --docs-pr-metadata "${DOCS_PR_METADATA_PATH}"
            --docs-pr-author "${DOCS_BOT_APP_SLUG}[bot]"
            --safe-outputs-result "${SAFE_OUTPUTS_RESULT}"
            --safe-outputs-items-failed "${SAFE_OUTPUTS_ITEMS_FAILED}"
            --expected-existing-draft "${RUNNER_TEMP}/pr-docs-check-context/existing-draft.json"
            --output "${RUNNER_TEMP}/pr-docs-check-outcome.json"
        - name: Publish trusted documentation outcome
          uses: actions/github-script@v9.0.0
          env:
            EXPECTED_SOURCE_REPOSITORY: ${{ github.event.inputs.source_repository }}
            EXPECTED_SOURCE_PR_NUMBER: ${{ github.event.inputs.pr_number }}
            CANONICAL_OUTCOME_PATH: ${{ runner.temp }}/pr-docs-check-outcome.json
          with:
            github-token: ${{ steps.docs-bot-token.outputs.token }}
            script: |
              const fs = require('fs');

              const marker = '<!-- aspnetcore-pr-docs-check -->';
              const docsAuthorMarker = '<!-- aspnetcore-pr-docs-check-author -->';
              const expectedRepository = process.env.EXPECTED_SOURCE_REPOSITORY;
              const expectedPrNumber = Number.parseInt(process.env.EXPECTED_SOURCE_PR_NUMBER, 10);

              if (expectedRepository !== 'dotnet/aspnetcore') {
                core.setFailed(`Unexpected source repository: ${expectedRepository}`);
                return;
              }

              if (!Number.isInteger(expectedPrNumber) || expectedPrNumber <= 0) {
                core.setFailed(`Invalid source PR number: ${process.env.EXPECTED_SOURCE_PR_NUMBER}`);
                return;
              }

              const outcome = JSON.parse(fs.readFileSync(process.env.CANONICAL_OUTCOME_PATH, 'utf8'));
              const renderKind = String(outcome.render_kind || 'invalid');
              const summary = String(outcome.summary || '').trim();
              const docsPrUrl = String(outcome.docs_pr_url || '').trim();
              const docsPrNumber = Number(outcome.docs_pr_number);

              if (!outcome.allow_comment) {
                core.setFailed(`Outcome validation failed: ${outcome.diagnostic || 'unknown reason'}`);
              }

              if (Number(outcome.source_pr_number) !== expectedPrNumber) {
                core.setFailed(`Canonical outcome targeted PR ${outcome.source_pr_number}; expected ${expectedPrNumber}.`);
                return;
              }

              const [owner, repo] = expectedRepository.split('/');
              const sourcePr = await github.rest.pulls.get({
                owner,
                repo,
                pull_number: expectedPrNumber,
              });

              const author = sourcePr.data.user;
              const sourceAuthor = author?.type === 'Bot' ? '' : (author?.login || '');
              const surfaces = (outcome.surfaces || []).map(surface =>
                `* **${surface.name}:** ${surface.required ? 'Required' : 'Not required'} — ${surface.reason}`);

              let heading;
              if (renderKind === 'restricted') {
                heading = 'ℹ️ This pull request wasn\'t processed automatically.';
              } else if (renderKind === 'drafted') {
                heading = `📝 Documentation ${outcome.docs_pr_action === 'updated' ? 'updated' : 'drafted'}: ${docsPrUrl}`;
              } else if (['draft_failed', 'drafted_missing_pr'].includes(renderKind)) {
                heading = '⚠️ Documentation appears necessary, but a draft PR could not be created.';
              } else if (renderKind === 'skipped') {
                heading = '✅ No documentation PR was created.';
              } else {
                heading = '⚠️ The documentation workflow returned an invalid or inconsistent result.';
              }

              const sourceComment = renderKind === 'restricted'
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
                    ...(Number.isInteger(outcome.confidence)
                      ? [`**Confidence that documentation is needed:** ${outcome.confidence}%`, '']
                      : []),
                    ...(summary ? [summary, ''] : []),
                    ...(surfaces.length ? ['**Documentation surfaces considered**', '', ...surfaces] : []),
                    ...(renderKind === 'invalid'
                      ? ['', `See the workflow run for validation details: ${process.env.GITHUB_SERVER_URL}/${process.env.GITHUB_REPOSITORY}/actions/runs/${process.env.GITHUB_RUN_ID}`]
                      : []),
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

              if (renderKind !== 'drafted' || !sourceAuthor || !Number.isInteger(docsPrNumber) || docsPrNumber <= 0) {
                return;
              }

              const authorComment = [
                docsAuthorMarker,
                `@${sourceAuthor}, this draft documents your source change in ${expectedRepository}#${expectedPrNumber}.`,
                '',
                'Please review it for technical accuracy and confirm that the user impact and recommended guidance match the implementation.',
              ].join('\n');
              const docsComments = await github.paginate(github.rest.issues.listComments, {
                owner: 'dotnet',
                repo: 'AspNetCore.Docs',
                issue_number: docsPrNumber,
                per_page: 100,
              });
              const priorAuthorComment = docsComments.find(comment => comment.body?.includes(docsAuthorMarker));
              if (priorAuthorComment) {
                await github.rest.issues.updateComment({
                  owner: 'dotnet',
                  repo: 'AspNetCore.Docs',
                  comment_id: priorAuthorComment.id,
                  body: authorComment,
                });
              } else {
                await github.rest.issues.createComment({
                  owner: 'dotnet',
                  repo: 'AspNetCore.Docs',
                  issue_number: docsPrNumber,
                  body: authorComment,
                });
              }

pre-agent-steps:
  - name: Check out trusted workflow helpers
    uses: actions/checkout@v7.0.1
    with:
      persist-credentials: false
      path: _workflow-source
      sparse-checkout: .github/workflows/pr-docs-check
      sparse-checkout-cone-mode: false
  - name: Mint ASP.NET Core docs bot token
    id: docs-bot-token
    uses: actions/create-github-app-token@v3.2.0
    with:
      client-id: ${{ secrets.ASPNETCORE_DOCS_BOT_CLIENT_ID }}
      private-key: ${{ secrets.ASPNETCORE_DOCS_BOT_PRIVATE_KEY }}
      owner: dotnet
      repositories: |
        aspnetcore
        AspNetCore.Docs
        AspNetCore.Docs.Automation
  - name: Resolve source version and existing docs draft
    env:
      GH_TOKEN: ${{ steps.docs-bot-token.outputs.token }}
      DOCS_BOT_APP_SLUG: ${{ steps.docs-bot-token.outputs.app-slug }}
      SOURCE_REPOSITORY: ${{ github.event.inputs.source_repository }}
      SOURCE_PR_NUMBER: ${{ github.event.inputs.pr_number }}
    run: |
      set -euo pipefail
      trap 'rm -rf -- _workflow-source' EXIT

      case "${SOURCE_REPOSITORY}" in
        dotnet/aspnetcore) ;;
        *)
          echo "ERROR: Unexpected source repository: ${SOURCE_REPOSITORY}" >&2
          exit 1
          ;;
      esac
      if ! [[ "${SOURCE_PR_NUMBER}" =~ ^[1-9][0-9]*$ ]]; then
        echo "ERROR: SOURCE_PR_NUMBER must be a positive integer." >&2
        exit 1
      fi

      CONTEXT_DIR=/tmp/gh-aw/pr-docs-check
      mkdir -p "${CONTEXT_DIR}"
      gh api "/repos/${SOURCE_REPOSITORY}/pulls/${SOURCE_PR_NUMBER}" \
        > "${CONTEXT_DIR}/source-pr.json"
      gh api --method GET --paginate --slurp "/repos/dotnet/aspnetcore/branches?per_page=100" \
        | jq '[.[][] | .name]' \
        > "${CONTEXT_DIR}/source-branches.json"
      python3 _workflow-source/.github/workflows/pr-docs-check/resolve_target_version.py \
        --policy _workflow-source/.github/workflows/pr-docs-check/version-policy.json \
        --pull-request "${CONTEXT_DIR}/source-pr.json" \
        --release-branches "${CONTEXT_DIR}/source-branches.json" \
        --output "${CONTEXT_DIR}/target-version.json"

      gh api --method GET --paginate --slurp \
        "/repos/dotnet/AspNetCore.Docs/pulls?state=open&base=main&per_page=100" \
        | jq '[.[][]]' \
        > "${CONTEXT_DIR}/open-docs-pulls.json"
      python3 _workflow-source/.github/workflows/pr-docs-check/find_existing_draft.py \
        --pull-requests "${CONTEXT_DIR}/open-docs-pulls.json" \
        --source-repository "${SOURCE_REPOSITORY}" \
        --source-pr-number "${SOURCE_PR_NUMBER}" \
        --target-repository dotnet/AspNetCore.Docs \
        --head-repository dotnet/AspNetCore.Docs.Automation \
        --allowed-author "${DOCS_BOT_APP_SLUG}[bot]" \
        --output "${CONTEXT_DIR}/existing-draft.json"

      if [ "$(jq -r '.found' "${CONTEXT_DIR}/existing-draft.json")" = "true" ]; then
        HEAD_REF="$(jq -r '.selected.head_ref' "${CONTEXT_DIR}/existing-draft.json")"
        git \
          -c credential.helper= \
          -c "credential.helper=!gh auth git-credential" \
          fetch --no-tags \
          "https://github.com/dotnet/AspNetCore.Docs.Automation.git" \
          "+refs/heads/${HEAD_REF}:refs/remotes/automation/${HEAD_REF}"
        git checkout -B "${HEAD_REF}" "refs/remotes/automation/${HEAD_REF}"
      fi
  - name: Preserve trusted draft context
    uses: actions/upload-artifact@v7.0.1
    with:
      name: pr-docs-check-context-${{ github.run_attempt }}
      path: /tmp/gh-aw/pr-docs-check/existing-draft.json
      if-no-files-found: error
      retention-days: 1

timeout-minutes: 20
---

# ASP.NET Core PR documentation check

Analyze pull request #${{ inputs.pr_number }} in `${{ inputs.source_repository }}` and decide whether it requires an update to the ASP.NET Core documentation in the current workspace, `dotnet/AspNetCore.Docs`.

This workflow is manually dispatched. Do not modify `dotnet/aspnetcore` or push directly to `dotnet/AspNetCore.Docs`. Before analysis, trusted pre-agent steps resolve the source version and search for an existing automated documentation draft. Trusted safe outputs push documentation branches to `dotnet/AspNetCore.Docs.Automation` and open or update draft pull requests against `dotnet/AspNetCore.Docs`. Your only permitted visible outcomes are:

1. When documentation confidence is at least 60%, one new or updated draft pull request in `dotnet/AspNetCore.Docs` and one `notify_source_pr` result.
2. When documentation confidence is below 60%, one `noop` result and one `notify_source_pr` result explaining why no documentation PR was created.
3. When documentation is required but drafting fails, one `notify_source_pr` result with `result: "draft_failed"`.
4. When the source PR is excluded by the security-concern rules, no docs changes, one generic `noop`, and one `notify_source_pr` result with `result: "restricted"`.

## Validate the request

Confirm that:

- `source_repository` is exactly `dotnet/aspnetcore`.
- `pr_number` is a positive integer.
- The pull request exists and is merged.

If the repository input is invalid or the pull request doesn't exist, emit `noop` with the validation failure and stop because there is no valid source PR to notify. If the pull request exists but isn't merged, emit `noop`, then emit `notify_source_pr` with confidence 0, `result: "skipped"`, `docs_pr_action: "none"`, all three surfaces set to not required because the change isn't eligible for analysis, and stop.

## Vulnerability-related changes are out of scope

This workflow must not assess, discuss, summarize, document, or make recommendations about potential vulnerabilities, abuse scenarios, exploitability, or impact. Before reading diff hunks, linked issues, review comments, or issue comments, inspect only the source PR's title, body, labels, author, milestone, base branch, merge state, and changed file names to determine whether this exclusion applies.

Treat the PR as restricted when its title, body, or labels explicitly present it as:

- a vulnerability or exploit fix;
- a CVE, GHSA, advisory, coordinated-disclosure, or MSRC-related change;
- a security fix intended to patch a reported weakness;
- a change whose public explanation could disclose vulnerability details.

Do not evaluate whether such a claim is valid. If the available high-level metadata explicitly suggests a vulnerability-related change but the classification is uncertain, treat the PR as restricted.

Do not infer that a PR is vulnerability-related from a security-sensitive product area or from words such as `security`, `secure`, `harden`, `restrict`, `validate`, `forgery`, `authentication`, or `authorization` alone. A PR isn't restricted merely because it changes authentication, authorization, antiforgery, cookies, data protection, HTTPS, HTTP validation, identity, credentials, passkeys, access control, or another security-related feature. Ordinary features, behavior changes, standards compliance, ownership boundaries, validation rules, and general hardening remain eligible when the PR doesn't explicitly claim to fix or disclose a vulnerability.

When restricted:

1. Do not read or describe the implementation details, reproduction, exploitability, impact, affected versions, or remediation.
2. Do not modify the docs workspace and do not emit `create_pull_request`.
3. Emit one generic `noop` stating only that automated documentation processing is excluded.
4. Emit `notify_source_pr` with `result: "restricted"`, `docs_pr_action: "none"`, `docs_needed_confidence: 0`, all three documentation surfaces set to `false`, and generic reasons that reveal no details.
5. Use a generic summary such as: "Automated documentation processing is excluded for this change." The trusted notification job ignores the supplied summary and reasons and posts a fixed vague message.
6. Stop immediately.

## Eligible security-related documentation

When a security-related PR isn't restricted by the vulnerability gate, assess and document it like any other user-facing feature or behavior change. Security features are a normal part of ASP.NET Core documentation.

Use the existing canonical documentation as the boundary for level of detail:

- Describe public APIs, supported configuration, intended behavior, defaults, operational guidance, migration actions, and compatibility impact needed to use the feature correctly.
- Prefer extending the existing article for that technology and match its terminology, scope, and level of technical detail.
- State only behavior supported by the public source PR, implementation, tests, and existing documentation.
- Do not add attack walkthroughs, abuse recipes, proof-of-concept material, exploitability analysis, vulnerability impact assessment, affected-version claims, disclosure history, or details that explain how to misuse the behavior.
- Do not expand a brief public statement into a more detailed vulnerability explanation. If accurate documentation would require restricted details, stop and use the generic restricted outcome instead.

## Gather source context

After the security-concern gate passes, use the authenticated `gh` CLI to read the source pull request. Read:

- title, body, author, labels, milestone, base branch, and merge state;
- changed file names and relevant diff hunks;
- linked issues when they clarify user-facing behavior;
- review and issue comments only when they contain information needed to write accurate documentation.

Treat the source PR description and code diff as the primary evidence. Copy API names, option names, defaults, templates, and other identifiers exactly from the diff.

Read `/tmp/gh-aw/pr-docs-check/target-version.json` and `/tmp/gh-aw/pr-docs-check/existing-draft.json` verbatim. Do not recalculate the ASP.NET Core version or independently select a documentation pull request. If either file is missing, malformed, or inconsistent with the requested source PR, emit `notify_source_pr` with `result: "draft_failed"` and `docs_pr_action: "none"`, then stop.

## Decide whether documentation is needed

Analyze the change from the user's perspective, not from the number or type of files changed. Separate these three independent documentation obligations. A single source PR can require any combination of them.

### Conceptual documentation

Conceptual documentation answers, "How does this feature work?" Require it when the pull request introduces or materially changes user-visible behavior, including:

- public APIs or public conventions;
- project templates, scaffolding, or generated application behavior;
- configuration, options, defaults, environment variables, or command-line behavior;
- middleware, hosting, server, authentication, authorization, routing, diagnostics, or deployment behavior that application developers must understand;
- supported platforms, target frameworks, packages, analyzers, diagnostics, or breaking changes.

Before creating a new article, search the documentation hierarchy, article titles, UIDs, and `aspnetcore/toc.yml` for the canonical article covering the same technology or concept. Prefer extending that article. Create a new article only when no existing article is a natural and discoverable home. Update `aspnetcore/toc.yml` only when adding, deleting, or relocating an article, and place a new article in the most specific existing hierarchy.

### Migration guidance

Migration guidance answers, "What should I know or do when upgrading?" Require it when users moving from the previous ASP.NET Core version should:

- adopt a newly recommended approach;
- remove, replace, simplify, or review existing configuration;
- understand a new default or observable behavior;
- opt in, opt out, or account for a compatibility or deployment consideration.

Migration guidance must be concise and task-oriented: explain what changed, who should care, what users should inspect or change, and the recommended action. Link to the canonical conceptual article for the full explanation instead of duplicating it. A migration recommendation does not by itself mean the change is breaking.

### Breaking-change documentation

Breaking-change documentation answers, "Can existing code or behavior stop working?" Require it only when there is concrete compatibility impact, such as:

- source or binary API incompatibility;
- removed APIs or configuration;
- an established default, convention, or semantic changing;
- previously successful operations failing;
- existing applications requiring remediation to preserve behavior.

Treat a `breaking-change` or similarly named label, an explicit breaking-change section, API removals, changed defaults, and tests demonstrating intentional behavior changes as evidence. Do not call a change breaking merely because it is new or because migration guidance is useful. When breaking impact is ambiguous, do not invent it; explain that human confirmation is needed.

### Confidence

Assign an integer `docs_needed_confidence` from 0 through 100 representing the confidence that at least one documentation surface is required.

Use objective evidence to establish these minimum confidence levels:

- public API added or materially changed: at least 70;
- project template or generated application behavior changed: at least 70;
- default, convention, or configuration behavior changed: at least 75;
- explicit breaking-change evidence: at least 80.

A score below 60 means no documentation PR may be created. A `noop` is appropriate only when the pull request is clearly limited to one of these categories or the available evidence is insufficient to reach 60:

- tests or test infrastructure;
- build, CI, repository automation, or dependency maintenance;
- internal refactoring with no public or observable behavior change;
- formatting, comments, or implementation-only cleanup;
- a bug fix that merely restores behavior already documented accurately.

Distinguish a confident "no docs needed" result from an ambiguous result. In the latter case, state what evidence is missing and recommend human review in the source PR notification.

## Write the documentation

Before editing, read:

- `.github/copilot-instructions.md`
- `.github/copilot-code-instructions.md` when adding or changing code samples

Also inspect relevant existing content under `aspnetcore/`. If the change is a .NET 11 What's New feature, read `.github/skills/whats-new-include-content-rules/SKILL.md` when that file is available and follow it.

Use the source version, moniker, previous version, migration directory, breaking-change directory, and release-note directory exactly as recorded in `/tmp/gh-aw/pr-docs-check/target-version.json`. This trusted resolver verifies the annually maintained `mainVersion` policy against current upstream `release/*` branches and fails before agent execution when a usable milestone disagrees.

The docs PR always targets `main`. Version placement is expressed through article monikers, moniker sections, migration directories, breaking-change directories, release-note directories, and versioned sample directories. Do not change an article-wide `monikerRange` merely because a newer feature is added. Wrap new-version material in a scoped moniker block such as:

```markdown
:::moniker range=">= aspnetcore-12.0"

New-version content.

:::moniker-end
```

When behavior differs between versions, preserve the earlier guidance in its own moniker range and add the new guidance in the resolved version's range.

Make the smallest complete documentation change across every required surface. Modify only files under `aspnetcore/`. Do not change repository instructions, workflows, dependency files, publishing configuration, or other root files.

## Create or update the draft pull request

Inspect `/tmp/gh-aw/pr-docs-check/existing-draft.json` before editing. It contains at most one selected trusted automated draft and may list older duplicates for human cleanup. Never create another pull request when `found` is `true`. When `blocked` is `true`, an existing matching pull request is no longer a draft; do not modify or replace it. Complete the analysis, then use `draft_failed` if documentation is required or `skipped` if it is not.

When no existing draft was found, after making and reviewing the documentation changes, emit `create_pull_request` exactly once with:

- branch: `docs/aspnetcore-pr-${{ inputs.pr_number }}`
- base: `main`
- a concise title without the `[docs]` prefix, because the workflow adds it;
- a body whose first line is `Source: ${{ inputs.source_repository }}#${{ inputs.pr_number }}`;
- the documentation confidence;
- separate conceptual, migration, and breaking-change decisions;
- a summary of the documentation change and a list of modified files.

Then emit `notify_source_pr` exactly once with `result: "drafted"`, `docs_pr_action: "created"`, no `existing_docs_pr_number`, the confidence score, all three surface decisions and reasons, and a concise summary. Do not use a closing keyword for the cross-repository source reference. Do not request reviewers. Do not retry a deterministic pull-request creation failure.

When an existing draft was found, the trusted pre-agent step has checked out its head branch. Update the documentation on that branch, then:

1. Emit `push_to_pull_request_branch` exactly once with `pull_request_number` set to `/tmp/gh-aw/pr-docs-check/existing-draft.json`'s selected number.
2. Emit `update_pull_request` exactly once for the same number, replacing its title with `[docs] ` followed by the current concise title and replacing its body with the same body structure required above.
3. Emit `notify_source_pr` exactly once with `result: "drafted"`, `docs_pr_action: "updated"`, `existing_docs_pr_number` set to the selected number, the confidence score, all three surface decisions and reasons, and a concise summary.

The trusted notification job independently verifies that the resulting pull request is open, draft, targets `main` in `dotnet/AspNetCore.Docs`, uses a head branch in `dotnet/AspNetCore.Docs.Automation`, has the required title prefix and label, carries the exact source marker, and is owned by the configured GitHub App bot. It obtains the source PR author directly from GitHub and creates or refreshes one author-notification comment on the docs PR.

If confidence is below 60, make no file changes and emit `noop` exactly once with:

- the no-documentation category used above;
- the changed files or evidence supporting that decision;
- a short explanation suitable for reviewing in the workflow run.

Then emit `notify_source_pr` exactly once with `result: "skipped"`, `docs_pr_action: "none"`, the confidence score, all three surface decisions and reasons, and a summary that clearly distinguishes "no docs needed" from "insufficient evidence."

If confidence is at least 60 but a draft PR operation cannot be requested, emit `notify_source_pr` exactly once with `result: "draft_failed"`, `docs_pr_action: "none"`, and explain the failure. Never report this condition as a successful `noop`.
