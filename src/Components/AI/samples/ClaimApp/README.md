# Components.AI claim application

This Interactive Server application exercises a complete vehicle claim flow with model-generated conversation, backend tools, UI actions, synchronized state, approvals, cancellation, accessible themes, image evidence, transcribed voice notes, and continuous live voice.

From the repository root:

```bash
source activate.sh
dotnet run --project src/Components/AI/samples/ClaimApp/ClaimApp.csproj --no-restore
```

The sample opens at `http://127.0.0.1:5099`.

Image and voice evidence is sent as `Microsoft.Extensions.AI.DataContent` with its original media type. Photos can be selected or dropped onto the composer. Up to six vehicle photos can be reviewed together so findings can be correlated across wide, close-up, and alternate-angle images.

The composer uses the reusable `MessageInput`, attachment, media, send, stop,
audio-capture, and live-speech components from
`Microsoft.AspNetCore.Components.AI`. ClaimApp only supplies claim-specific image
validation, evidence limits, prompts, transcription, and workflow state.

The claim assistant requires a configured Microsoft Foundry resource. Configure one Azure OpenAI resource endpoint:

```bash
export AZURE_AI_FOUNDRY_ENDPOINT="https://<resource>.openai.azure.com"
export CLAIM_VISION_API_KEY="..."
export CLAIM_VISION_MODEL="gpt-5-mini"
export CLAIM_TRANSCRIPTION_MODEL="gpt-4o-mini-transcribe"
export CLAIM_RESEARCH_COUNTRY="US"
```

The same values can be stored outside the repository with development user
secrets:

```bash
dotnet user-secrets --project src/Components/AI/samples/ClaimApp/ClaimApp.csproj \
  set "AzureAI:Foundry:Endpoint" "https://<resource>.openai.azure.com"
dotnet user-secrets --project src/Components/AI/samples/ClaimApp/ClaimApp.csproj \
  set "AzureAI:Foundry:ApiKey" "..."
```

The app binds configuration through `IConfiguration`, so environment variables,
development user secrets, and standard ASP.NET Core configuration providers all
work. It derives the chat-completions, transcription, and Responses API routes
from the resource endpoint and uses the `api-key` header automatically. The
model handles ordinary conversation and decides whether a turn adds evidence that
should start structured damage analysis. The analyzer returns structured damage
findings, affected vehicle areas, confidence, human-review guidance, and the next
most useful photo to collect. It only flags visible or reported damage and does
not claim to diagnose hidden mechanical or structural damage. Missing or failed
Foundry configuration is surfaced as an error instead of simulated output.

The UI uses `AGUIChatClient` to send each turn to the app's `/claim-agent`
endpoint over HTTP and receive the response as an AG-UI Server-Sent Events
stream. The endpoint converts model updates through `AGUI.Server`, including
run lifecycle, text, tool calls and results, approval interrupts, errors, and
state snapshots or JSON Patch deltas. Tests replace only
`IClaimAssistantBackend`, so the AG-UI client, serialization, HTTP/SSE boundary,
and server conversion remain active.

By default, the server-side AG-UI client resolves `claim-agent` against the
browser-visible application base URI. Hosts that use a separate internal origin
can set `ClaimAgent:BaseAddress` to an absolute HTTP or HTTPS base URI. A path
prefix in the configured base address is preserved.

The full workflow transcribes recorded claim descriptions as soon as recording
stops, analyzes all submitted photos, estimates a repair range, finds likely
replacement parts, and returns public source links through Foundry web search.
Live voice uses the browser speech-recognition service to show interim text and
automatically submit finalized utterances through the same claim agent and tool
flow. Assistant responses are displayed in chat without spoken playback, and
recognition resumes for the next turn until live voice is explicitly stopped.
Browser, voice, and language availability varies, and the browser vendor can
process live microphone audio. Recorded voice notes continue to use the
configured Foundry transcription deployment. Foundry credentials remain on the
server and are never sent to browser JavaScript.

Parts and repair costs are intake estimates only. Fitment, labor, calibration,
hidden damage, taxes, and the final scope require professional verification. Web
search uses Grounding with Bing and can send query data outside the configured
resource's compliance and geographic boundary.
