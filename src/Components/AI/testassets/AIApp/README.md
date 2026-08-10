# AIApp manual live capture and replay

AIApp uses `EchoChatClient` unless one of the following manual modes is explicitly enabled.
Ordinary builds and tests never contact Azure or write recordings.

## Capture a decoded live response

Authenticate with the Azure CLI, choose an absolute artifact path outside this source
directory, and set these variables only on the AIApp server process:

```text
COMPONENTS_AI_CAPTURE_LIVE=true
COMPONENTS_AI_AZURE_OPENAI_ENDPOINT=<endpoint>
COMPONENTS_AI_AZURE_OPENAI_DEPLOYMENT=<deployment>
COMPONENTS_AI_CAPTURE_PATH=<absolute-artifact-path>
```

The app uses `AzureOpenAIClient` with `DefaultAzureCredential` configured for the current
Azure CLI login. It writes only decoded chat messages and response updates after a call
completes. The write is rejected if the serialized recording contains the configured
endpoint, deployment, or credential-like markers.

## Replay the captured response offline

Start a new AIApp process without the live-capture or Azure variables and set:

```text
COMPONENTS_AI_MANUAL_REPLAY_PATH=<absolute-artifact-path>
```

The replay client does not use Azure. It requires the current user prompt to exactly match
the captured prompt and replays the decoded updates in their original order.
