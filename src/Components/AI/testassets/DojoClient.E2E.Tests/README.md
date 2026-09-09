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
