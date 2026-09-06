# ASP.NET Core issue triage canvas

This project-scoped canvas displays complete, read-only issue triage queues for
the public ASP.NET Core area taxonomy. It defaults to `area-blazor`.

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

Selecting one issue can queue the exact project `investigate-issue` skill. The
extension revalidates the server-owned issue identity and live queue membership
before dispatch, passes no issue-authored text as instructions, and runs a
short-lived isolated session using a locally configured model with only bounded
public read-only evidence tools. At investigation invocation time, the local
`ASPNETCORE_ISSUE_TRIAGE_MODEL` environment value takes precedence when set;
otherwise the extension reads the fixed session-local setting at
`files/aspnetcore-issue-triage/model-settings.json` under the SDK-provided
session workspace. The setting file is extension-internal and its path is never
selected by callers or issue content. Both sources must contain a supported
non-Anthropic model identifier; missing or invalid configuration fails closed
with no default or fallback. Repository source and history tools resolve refs
through public GitHub before reading tracked content. The advisory report is
stored under the current Copilot session's `files/` artifacts outside the
repository checkout. The extension does not comment, label, assign, close, edit
projects, run reporter code, or perform any other GitHub mutation.

Private investigation diagnostics are disabled by default. They can be enabled
only with the fixed session-local extension setting at
`files/aspnetcore-issue-triage/diagnostic-settings.json`; captures remain in
that session's private artifact root, are never rendered as the advisory
report, and are not published in the PR.
