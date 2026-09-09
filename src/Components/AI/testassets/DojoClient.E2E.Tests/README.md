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
test with `[DataRow("AGUI")]` and `[DataRow("Direct")]`:

```csharp
[TestMethod]
[DataRow("AGUI")]
[DataRow("Direct")]
public async Task Scenario_ExercisesComponentBehavior(string backend)
{
    var (ui, model) = await StartDojoAsync(
        backend, options => options.ConfigureServices<DojoModelOverrides>(
            nameof(DojoModelOverrides.AgenticChat)));
    var checkpoints = new ApiCheckpointClient(model);
    var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(ui));
    var page = await context.NewPageAsync();
    // Navigate to the shared page, interact, and assert component behavior.
}
```

Keep backend selection out of page markup and test assertions. `StartDojoAsync`
starts the API only for AG-UI rows and applies the same recorded-model override
to whichever host owns the model. Checkpoints target that host too. Direct rows
use an unreachable AG-UI URL so accidental transport use fails rather than
silently reaching another server.

Model overrides must replace the model registration, not the UI's scenario
client. `DojoModelOverrides` handles the unkeyed API model and keyed direct model.
The recordings and their request assertions are shared across both runs.
Native tool results are compared in the recording's JSON representation, so
transport encoding differences do not require separate recordings.
