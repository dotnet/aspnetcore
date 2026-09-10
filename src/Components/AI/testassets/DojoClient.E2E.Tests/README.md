# Dojo backend coverage

DojoClient uses the same scenario pages with either backend, selected at app startup:

| `DOJO_BACKEND` | Model host | Pipeline |
| --- | --- | --- |
| `AGUI` (default) | AGUIDojoApi | `AGUIChatClient` over HTTP/SSE |
| `Direct` | DojoClient | In-process `IChatClient`, without the AG-UI transport |

For manual use, set `DOJO_BACKEND` in the DojoClient process environment or pass
`--DOJO_BACKEND=Direct` as an application argument. AG-UI mode also uses
`AGUI_DOJO_API_URL` (default `http://localhost:5018`). Direct mode does not require
an API process. Both backends use the offline scripted model unless explicitly
configured with `OPENAI_BASE_URL` or `OPENAI_API_KEY` in the model host.
The sibling `DojoAgent` library owns the shared models, tools, and prompts; neither
web app references the other.

Backend selection is represented by the shared `DojoBackendKind` enum. Scenario
pages use the stateless `IDojoScenarioBridge` to construct request options without
inspecting the backend. Each agent creates its own backend-neutral `DojoStateUpdates`
mapper, so reset agents do not share mutable tool-correlation state.
The original dojo endpoint names are shared through `DojoScenarioEndpoints`.

## Suite-wide hosts and per-test sessions

The suite lazily starts at most three application processes: one AGUIDojoApi
host, one AG-UI-configured DojoClient host, and one Direct-configured DojoClient
host. `TestRoot.Servers` caches these fixed configurations until assembly cleanup.
Filtered runs start only the hosts they need.

`GetDojoAsync` acquires those hosts and creates a `DojoTestSession`, not another
process. The session selects a `DojoRecording` or the offline scripted model.
Recordings no longer participate in process environment or service-override
cache keys.

Each session has a unique ID, its own recorded-script expectations, checkpoint
gates, model pipeline, and cancellation lifetime. `GetScenarioUrl` places the ID
in the scenario URL. A test-only UI client decorator reads it through the
circuit's `NavigationManager` and forwards it as native chat-option metadata or
AG-UI `ForwardedProperties`. This does not rely on `HttpContext` being available
during interactive rendering, and does not add test parameters to scenario pages.

The model host routes each request to that session's model. Missing or expired
IDs fail rather than falling back to another recording. Checkpoint releases are
session-scoped, so identical prompts in different sessions cannot unblock each
other. Test cleanup cancels and drains pending requests, disposes the model, and
removes its state without stopping shared hosts. Cancellation/draining has a
30-second deadline: an abandoned enumerator fails cleanup explicitly instead of
hanging the suite.

Tests still create fresh browser contexts. `WithServerRouting` selects the
appropriate shared UI instance through the proxy's `X-Test-Backend` header.
Server configuration never changes between rows.

## Consolidated scenarios

DojoClient is the UI test app for both backends. The suite includes the original
dojo scenarios and the focused component scenarios previously hosted in AIApp:

- `/function-approval` exercises `ApprovalRequiredAIFunction` with the built-in
  approval UI. Tests inspect the model host's per-conversation invocation count
  to distinguish approval from rejection.
- `/function-invocation` exercises the generic `FunctionInvocationContentBlock`,
  including its informational flag and loading-to-result transition. The release
  button unblocks the real server tool, regardless of which host executes it.
- `/rich-text` renders native `RichTextContent` snapshots, including tables,
  images, footnotes, task lists, and encoded HTML, rather than parsing Markdown.

Function scenario controls use the page's conversation ID, so concurrent pages
do not share invocation counters or result gates. The tests remove their control
state when they finish.

The structured-rich-text and informational-invocation scenarios use a dojo
custom AG-UI event containing the serialized native chat update. This preserves
typed rich-text trees and renders informational calls before their results;
the standard AG-UI client buffers tool calls until a result or interrupt arrives.
The payload crosses the real HTTP/SSE transport and is decoded by the UI adapter.
Direct mode consumes the native update without that encoding. Approval scenarios
continue to use the standard AG-UI approval protocol.

## Adding a scenario

Create one page in DojoClient and one test in this project. Derive the test class
from `DojoTestBase`, retain `[UITest]` on the partial class, and parameterize the
test with `[DojoBackends]`. The attribute supplies a separately reported row for
each supported `DojoBackendKind`:

```csharp
[TestMethod]
[DojoBackends]
public async Task Scenario_ExercisesComponentBehavior(DojoBackendKind backend)
{
    var dojo = await GetDojoAsync(backend, DojoRecording.AgenticChat);
    var checkpoints = dojo.Checkpoints;
    var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(dojo.UI));
    var page = await context.NewPageAsync();
    await page.GotoAsync(dojo.GetScenarioUrl("/agentic_chat"));
    await page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
    // Interact and assert component behavior.
}
```

Keep backend selection out of page markup and test assertions. `GetDojoAsync`
acquires the API only for AG-UI rows. Always navigate with the session's
`GetScenarioUrl`, including additional pages or browser contexts in the test.
Omit the recording argument for scripted or dedicated fixed scenarios. Direct rows
use an unreachable AG-UI URL so accidental transport use fails rather than
silently reaching another server.

`DojoModelOverrides` installs the same model router once in each host, leaving
the scenario client and transport intact. `DojoRunStore` owns per-session models.
The recordings and their request assertions are shared across both runs.
Native tool results are compared in the recording's JSON representation, so
transport encoding differences do not require separate recordings.

The forwarding decorator preserves AG-UI's generated thread-ID metadata when it
clones request options. Tests cover both generated IDs and explicit thread/state
metadata through the real `UIAgent` and `AGUIChatClient` request builders.
