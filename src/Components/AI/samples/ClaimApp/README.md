# Components.AI claim application

This Blazor sample demonstrates multimodal claim intake over AG-UI with Microsoft Foundry chat, vision, transcription, and web search.

Configure Azure OpenAI and run from the repository root:

```bash
export AZURE_OPENAI_ENDPOINT="https://<resource>.openai.azure.com"
export AZURE_OPENAI_API_KEY="..."

source activate.sh
dotnet run --project src/Components/AI/samples/ClaimApp/ClaimApp.csproj --no-restore
```

The sample uses `gpt-5-mini` for chat and vision and `gpt-4o-mini-transcribe` for recorded audio. Override them with `AZURE_OPENAI_CHAT_DEPLOYMENT` and `AZURE_OPENAI_TRANSCRIPTION_DEPLOYMENT`.
