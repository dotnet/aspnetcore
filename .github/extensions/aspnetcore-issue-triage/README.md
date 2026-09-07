# ASP.NET Core issue triage canvas

This project-scoped canvas displays complete public ASP.NET Core issue queues
with local investigation drafts, bounded read-only discussion, and explicit
human-confirmed issue comments. It defaults to `area-blazor`.

Queue membership is mechanical:

```text
repo:dotnet/aspnetcore is:issue is:open no:milestone label:<selected-area>
-label:"Needs: Author Feedback"
-label:":heavy_check_mark: Resolution: Answered"
-label:":heavy_check_mark: Resolution: Duplicate"
sort:created-asc
```

The extension retrieves every open issue carrying the selected area label with
GraphQL pagination, then applies the milestone and exclusion-label predicates.
It reports source totals, retrieved totals, qualifying totals, pagination
coverage, rate-limit state, and any limitation instead of silently truncating.
Issues whose label membership cannot be established are omitted from the
qualifying queue and counted as unknown rather than receiving an affirmative
membership explanation.

Configure the isolated non-Anthropic model with the
`ASPNETCORE_ISSUE_TRIAGE_MODEL` environment variable, or with the
session-private `files/aspnetcore-issue-triage/model-settings.json` artifact.
The artifact uses this schema, with a locally chosen supported model identifier:

```json
{
  "schemaVersion": "1.0.0",
  "model": "<non-Anthropic model identifier>"
}
```

The environment variable takes precedence over the session-local artifact.
Missing or invalid configuration fails closed.

Selecting one issue can queue the exact project `investigate-issue` skill. The
extension revalidates the server-owned issue identity and live queue membership
before dispatch, passes no issue-authored text as instructions, and runs a
short-lived isolated session using a locally configured non-Anthropic model with
only bounded public read-only evidence tools. Missing or invalid model
configuration fails closed with no default or fallback. Repository source and
history tools resolve refs through public GitHub before reading tracked content.
The advisory report and editable draft are stored under the current Copilot
session's private artifacts outside the repository checkout. Saving, editing,
discussion, and preview do not write to GitHub. A person may edit the Markdown
draft, ask a bounded read-only follow-up question, and use one-step Undo to
restore the previous saved draft. Saved issues can be reopened after they leave
the current queue.

Preview shows the exact current Markdown and requires an explicit confirmation
before publishing. The first publication creates one issue comment; later
publication updates only that canvas-created comment. Unknown create or update
outcomes remain pending and Reconcile performs a read-only recovery instead of
retrying the write. The extension does not label, assign, close, edit projects,
run reporter code, or perform any other GitHub mutation.

Create recovery only adopts a comment when the bounded public evidence uniquely
identifies it as newer than the recorded attempt, with the expected body and
authenticated account. GitHub timestamps have one-second precision, so a
same-second candidate is indistinguishable from an older comment and remains
unknown rather than being adopted.

Publication is guarded by the canonical public issue, authenticated human
account, exact draft revision and body, bounded public Markdown checks, and
remote-comment comparison. GitHub does not provide an atomic compare-and-swap
for comment updates, so the remote read-before-write comparison cannot eliminate
every external write race. Unknown network outcomes remain pending for manual
reconciliation rather than being retried automatically.

Private investigation diagnostics are disabled by default. If explicitly
enabled, captures remain in the session's private artifact root, are never
rendered as the advisory report, and are not published.
