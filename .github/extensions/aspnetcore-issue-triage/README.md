# ASP.NET Core issue triage canvas

This project-scoped canvas displays complete public ASP.NET Core issue queues
and requests research in the current session or a fresh nested child. It defaults
to `area-blazor`. Its handoff follows the small queue-to-session pattern used
by the [Aspire issue triage canvas](https://github.com/dotnet/aspire/blob/1dd4584e3df56f5544a3e7f5fda8aa767f92318e/.github/extensions/issue-triage-canvas/extension.mjs#L970-L1035)
and [Team App foreground dispatch](https://github.com/dotnet/aspire/blob/1dd4584e3df56f5544a3e7f5fda8aa767f92318e/.github/extensions/aspire-team-app/extension.mjs#L236-L278)
(MIT-licensed source). The prompt uses the installed host's nested `kickoff`
tool arguments rather than the older issue-board argument spelling.

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

Both investigation buttons revalidate the selected issue's canonical public
identity and current queue membership, then send a fixed request to the
foreground session:

- **Investigate here** invokes `investigate-issue` in the current conversation,
  using its existing checkout and model settings.
- **Investigate in new child session** asks the foreground agent to use
  `create_session` with an interactive, research-only `investigate-issue`
  kickoff. This creates a fresh worktree session nested under the current
  session, not an existing issue session. It keeps the current project, sets
  `detached: false` and `coordinate_with_creator: false`, and supplies no
  base-branch or model override.

Issue title, body, and comments never
become prompt instructions. The receipt means only **Request sent to chat**;
the canvas does not claim that a child session was created or research finished.

The child session uses its ordinary host model settings, not necessarily the
foreground session's selected model. Before researching, either destination
must confirm that `investigate-issue` is available in its checkout and stop if
the prerequisite is missing. For a fresh child, this normally requires the
skill to have landed on the repository default branch. The canvas has no model setting, report
storage, editor, discussion, preview, publishing, or GitHub-write surface.
Area preference is the sole durable canvas artifact.

## Dispatch and permissions

A busy foreground agent processes the request after its current work. The
canvas does not poll the child, parse chat answers, or synchronize research
results back into the queue. Closing or refreshing the canvas cannot cancel a
request already delivered to chat. A failed or missing send receipt is reported
as unconfirmed when delivery may have occurred; inspect chat before deliberately
using the same button again. Both buttons are disabled while either request is
in flight; the receipt identifies its destination. Refresh/reload never resends
automatically.

The loopback endpoints retain capability-token authentication, same-origin POST
checks, bounded request bodies and text-only rendering of issue-authored data.
Queue snapshots include partial-retrieval limitations and unknown membership.
The extension has no GitHub mutation endpoint. Research in the destination
session uses that session's tools and permissions: the skill's public/read-only
instructions are not an isolated runtime enforcement boundary.

## Development

Run `node --test .github/extensions/aspnetcore-issue-triage/*.test.mjs`.
The suite covers queue paging, canonical validation, real HTTP/SSE lifecycle,
foreground-dispatch recording seams, stale completion handling, and retired-route
rejection. Recording a send does not establish that the foreground agent opened
a new child session or that either destination produced research; observe
those separately in the app with the required skill available in that checkout.

The former saved-report/editor/discussion/publishing workflow has been removed.
Existing private report, workspace, diagnostic and model-setting files are not
read, migrated, or deleted by this extension.
