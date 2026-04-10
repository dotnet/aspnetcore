---
on:
  pull_request:
    types: [labeled]
    names: [community-contribution]
    forks: ["*"]

description: >
  Check whether community contribution pull requests reference an associated
  issue in this repository and comment with contribution guidance when they do not.

permissions:
  contents: read
  issues: read
  pull-requests: read

tools:
  github:
    toolsets: [issues, pull_requests]

safe-outputs:
  noop:
    report-as-issue: false
  add-comment:
    hide-older-comments: true
---

# Community PR Issue Check

You are reviewing pull request #${{ github.event.pull_request.number }} in
`${{ github.repository }}`. This pull request has been labeled
`community-contribution`.

Your task is to determine whether the PR body references a related GitHub issue
in this repository. This is required by the repository's contribution policy.

## Instructions

1. Read the body of pull request #${{ github.event.pull_request.number }} using
   the GitHub pull request tools.

2. Look for references to GitHub issues in `${{ github.repository }}`.
   Consider these examples as valid formats:
   - Keyword-linked references such as `Fixes #123`, `Closes #123`,
     `Resolves #123`, `Addresses #123`, or `Related to #123`
   - Direct URL references such as
     `https://github.com/${{ github.repository }}/issues/123`
   - Plain `#123` references when the surrounding text clearly indicates the PR
     is associated with that issue

3. For each candidate reference you find, verify through the GitHub issue tools
   that the referenced number is an issue in `${{ github.repository }}` and not
   a pull request.

4. If at least one valid repository issue reference exists, use the `noop` safe
   output with a short message explaining that the PR already references a valid
   issue.

5. If no valid repository issue reference exists, use the `add-comment` safe
   output to post this exact comment on the pull request:

   👋 Hi there, thanks for your contribution!

   Community pull requests in `${{ github.repository }}` need to have an associated issue before we can review them.

   Please update the pull request description to link the issue. For example: Fixes #123

   If an issue does not exist yet, please open one first: https://github.com/${{ github.repository }}/issues/new/choose

   For more details, see:
   - https://github.com/${{ github.repository }}/blob/main/CONTRIBUTING.md
   - https://github.com/${{ github.repository }}/blob/main/.github/pull_request_template.md

6. Do not modify labels, do not edit the pull request body, and do not analyze
   code changes. Only determine whether there is an associated issue reference
   and comment when one is missing.
