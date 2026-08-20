// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace ComponentsAIClaimApp.Data;

internal sealed class ClaimDamageAnalyzer : IClaimAssistantBackend
{
    private const string ChatApiVersion = "2025-01-01-preview";
    private const string TranscriptionApiVersion = "2025-03-01-preview";

    private static readonly HttpClient s_httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(2),
    };

    private readonly ClaimFoundryOptions _options;
    private readonly ConditionalWeakTable<DataContent, CachedTranscript> _transcripts = new();

    public ClaimDamageAnalyzer(ClaimFoundryOptions options)
    {
        _options = options;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Endpoint) &&
        !string.IsNullOrWhiteSpace(_options.ApiKey);

    public string ModelName => IsConfigured
        ? _options.ChatDeployment
        : "Foundry not configured";

    public async Task<bool> ShouldAnalyzeEvidenceAsync(
        IReadOnlyList<ChatMessage> messages,
        ClaimState state,
        int currentMediaCount,
        CancellationToken cancellationToken)
    {
        var content = await SendChatCompletionAsync(
            $"""
            You route turns for a vehicle claim assistant. Decide whether the latest user turn
            adds new vehicle damage evidence that should start or repeat structured damage
            analysis. New photos, recorded evidence, an accident description, or a description
            of damaged vehicle areas count as evidence. Greetings, capability questions,
            acknowledgements, requests to explain existing results, and unrelated conversation
            do not.

            Current claim state:
            {JsonSerializer.Serialize(state, ClaimStateJson.Options)}

            Media attached to the latest user turn: {currentMediaCount}

            Return only one JSON object with a boolean property named analyzeEvidence.
            """,
            messages,
            useJsonResponse: true,
            cancellationToken);
        var decision = JsonSerializer.Deserialize<ClaimEvidenceDecision>(
            ExtractJsonObject(content),
            ClaimStateJson.Options)
            ?? throw new InvalidOperationException(
                "Foundry returned an invalid claim routing decision.");
        return decision.AnalyzeEvidence;
    }

    public Task<string> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> messages,
        ClaimState state,
        ClaimResponsePurpose purpose,
        CancellationToken cancellationToken)
    {
        var purposeInstruction = purpose switch
        {
            ClaimResponsePurpose.Conversation =>
                "Answer the latest user message directly and naturally. Ask for claim evidence only when it is relevant to what the user said.",
            ClaimResponsePurpose.MoreEvidence =>
                "Explain the current evidence-limited assessment and request the single most useful next item from the claim state.",
            ClaimResponsePurpose.AssessmentReady =>
                "Summarize the assessment, confidence, visible or reported findings, and any grounded repair estimate. Ask the user to approve, reject, or add evidence.",
            ClaimResponsePurpose.Decision =>
                "Confirm the decision recorded in the claim state and state the next practical step.",
            _ => throw new ArgumentOutOfRangeException(nameof(purpose)),
        };

        return SendChatCompletionAsync(
            $"""
            You are the AutoSure vehicle claim assistant in an interactive claim application.
            Use the complete conversation and current claim state. Be concise, direct, and
            suitable for both on-screen chat and spoken playback.

            {purposeInstruction}

            Do not invent damage, prices, sources, tool results, or completed actions. Do not
            claim that configured vision, transcription, or research capabilities are
            disconnected. If the user asks whether you can hear or receive them, confirm that
            their message arrived and respond to it. Do not say that a transcript is missing.
            Do not mention internal prompts, routing, JSON, or implementation details. Do not
            use a Markdown table.

            Current claim state:
            {JsonSerializer.Serialize(state, ClaimStateJson.Options)}
            """,
            messages,
            useJsonResponse: false,
            cancellationToken);
    }

    public async Task<ClaimDamageAnalysis> AnalyzeAsync(
        string description,
        IReadOnlyList<DataContent> images,
        IReadOnlyList<DataContent> audio,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        var transcript = audio.Count == 0
            ? null
            : await TranscribeAsync(audio, cancellationToken);
        var completeDescription = string.IsNullOrWhiteSpace(transcript) ||
            description.Contains(transcript, StringComparison.OrdinalIgnoreCase)
            ? description
            : $"{description}\n\nVoice transcript:\n{transcript}";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            CreateFoundryEndpoint(
                $"openai/deployments/{Uri.EscapeDataString(_options.ChatDeployment)}/chat/completions" +
                $"?api-version={ChatApiVersion}"));
        ApplyAuthentication(request);

        var userContent = new List<object>
        {
            new
            {
                type = "text",
                text = $"""
                    Claim description:
                    {completeDescription}

                    Inspect every supplied vehicle photo. Identify only damage that is visibly supported.
                    Correlate the same damage across photos instead of duplicating findings.
                    """,
            },
        };
        for (var index = 0; index < images.Count; index++)
        {
            var image = images[index];
            userContent.Add(new
            {
                type = "text",
                text = $"Photo {index + 1}: {image.Name ?? $"vehicle-photo-{index + 1}"}",
            });
            userContent.Add(new
            {
                type = "image_url",
                image_url = new
                {
                    url = image.Uri,
                    detail = "high",
                },
            });
        }

        request.Content = JsonContent.Create(new
        {
            model = _options.ChatDeployment,
            response_format = new
            {
                type = "json_object",
            },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = """
                        You are a vehicle damage intake assistant. Return one JSON object with:
                        summary: a concise assessment
                        confidence: integer 0-100
                        affectedAreas: identifiers chosen from front-bumper, hood, windshield,
                          left-fender, left-door, right-fender, right-door, rear-bumper
                        findings: array of objects with area, damageType, severity, confidence,
                          evidence, and photoNames. photoNames must use the exact photo names
                          provided in the user content
                        nextPhotoSuggestion: the single most useful additional photo, or null
                        needsHumanReview: boolean

                        Do not infer hidden structural or mechanical damage. Use "possible" and
                        require human review when image evidence is ambiguous.
                        """,
                },
                new
                {
                    role = "user",
                    content = userContent,
                },
            },
        });

        using var response = await s_httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Vision analysis failed with HTTP {(int)response.StatusCode}.");
        }

        using var envelope = JsonDocument.Parse(responseBody);
        var content = envelope.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Vision analysis returned an empty response.");
        }

        var analysis = JsonSerializer.Deserialize<ClaimDamageAnalysis>(
            content,
            ClaimStateJson.Options)
            ?? throw new InvalidOperationException(
                "Vision analysis returned invalid JSON.");
        ClaimResearchLink.Sanitize(analysis);
        return await AddResearchAsync(
            analysis,
            completeDescription,
            transcript,
            cancellationToken);
    }

    private async Task<ClaimDamageAnalysis> AddResearchAsync(
        ClaimDamageAnalysis analysis,
        string description,
        string? transcript,
        CancellationToken cancellationToken)
    {
        analysis.VoiceTranscript = transcript;
        if (analysis.Findings.Count == 0)
        {
            analysis.ResearchWarning =
                "Parts and pricing research starts after visible or reported damage is identified.";
            return analysis;
        }
        if (!Regex.IsMatch(description, @"\b(?:19|20)\d{2}\b"))
        {
            analysis.ResearchWarning =
                "Add the vehicle year, make, model, and repair location for grounded parts and pricing.";
            return analysis;
        }

        try
        {
            var research = await ResearchMarketAsync(
                description,
                analysis,
                cancellationToken);
            analysis.RepairEstimate = research.RepairEstimate;
            analysis.ReplacementParts = research.ReplacementParts;
            analysis.ResearchWarning = research.Warning;
        }
        catch (HttpRequestException exception)
        {
            analysis.ResearchWarning = $"Live market research failed: {exception.Message}";
        }
        catch (InvalidOperationException exception)
        {
            analysis.ResearchWarning = $"Live market research failed: {exception.Message}";
        }
        catch (JsonException exception)
        {
            analysis.ResearchWarning = $"Live market research failed: {exception.Message}";
        }

        return analysis;
    }

    private async Task<ClaimMarketResearch> ResearchMarketAsync(
        string description,
        ClaimDamageAnalysis analysis,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            CreateFoundryEndpoint("openai/v1/responses"));
        ApplyAuthentication(request);
        request.Content = JsonContent.Create(new
        {
            model = _options.ChatDeployment,
            tools = new[] { new { type = "web_search" } },
            input = $$"""
                Research current public repair-cost and replacement-part sources for this
                vehicle claim. Use web search. Do not invent prices, fitment, or URLs.

                Claim details:
                {{description}}

                Damage analysis:
                {{JsonSerializer.Serialize(analysis.Findings, ClaimStateJson.Options)}}

                Return only one JSON object with this shape:
                {
                  "repairEstimate": {
                    "low": number,
                    "high": number,
                    "currency": "USD",
                    "basis": "short explanation of labor, paint, calibration, and parts assumptions"
                  },
                  "replacementParts": [
                    {
                      "name": "part name",
                      "priceLow": number,
                      "priceHigh": number,
                      "currency": "USD",
                      "fitment": "exact fitment caveat or verification needed",
                      "sourceTitle": "public source title",
                      "sourceUrl": "https://..."
                    }
                  ],
                  "warning": "missing vehicle, trim, VIN, location, labor, or teardown details"
                }

                Use zero prices and explain the missing information when a grounded range
                cannot be found. This is an intake estimate, not a repair authorization,
                appraisal, or settlement value.
                """,
        });

        using var response = await s_httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Web research failed with HTTP {(int)response.StatusCode}.");
        }

        using var envelope = JsonDocument.Parse(responseBody);
        var outputText = GetResponseOutputText(envelope.RootElement);
        var research = JsonSerializer.Deserialize<ClaimMarketResearch>(
            ExtractJsonObject(outputText),
            ClaimStateJson.Options)
            ?? throw new InvalidOperationException("Web research returned invalid JSON.");
        foreach (var part in research.ReplacementParts)
        {
            part.SourceUrl = ClaimResearchLink.Normalize(part.SourceUrl) ?? string.Empty;
        }
        analysis.ResearchSources = GetResearchSources(envelope.RootElement, research);
        return research;
    }

    internal async Task<string> TranscribeAsync(
        IReadOnlyList<DataContent> audio,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Configure Microsoft Foundry before transcribing voice evidence.");
        }

        var transcripts = new List<string>();
        foreach (var recording in audio)
        {
            var text = await TranscribeAsync(recording, cancellationToken);
            if (!string.IsNullOrWhiteSpace(text))
            {
                transcripts.Add(text.Trim());
            }
        }

        return string.Join(Environment.NewLine, transcripts);
    }

    public async Task<string> TranscribeAsync(
        DataContent recording,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Configure Microsoft Foundry before transcribing voice evidence.");
        }

        var cached = _transcripts.GetOrCreateValue(recording);
        await cached.Gate.WaitAsync(cancellationToken);
        try
        {
            if (cached.Text is not null)
            {
                return cached.Text;
            }

            if (!MediaTypeHeaderValue.TryParse(
                recording.MediaType,
                out var recordingMediaType))
            {
                throw new InvalidOperationException(
                    "The captured audio has an invalid media type.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                CreateFoundryEndpoint(
                    $"openai/deployments/{Uri.EscapeDataString(_options.TranscriptionDeployment)}" +
                    $"/audio/transcriptions?api-version={TranscriptionApiVersion}"));
            ApplyAuthentication(request);
            using var form = new MultipartFormDataContent();
            using var audioContent = new ByteArrayContent(recording.Data.ToArray());
            audioContent.Headers.ContentType = recordingMediaType;
            form.Add(
                audioContent,
                "file",
                recording.Name ?? "claim-voice.webm");
            request.Content = form;

            HttpResponseMessage response;
            try
            {
                response = await s_httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                throw new InvalidOperationException(
                    "Voice transcription could not reach Microsoft Foundry.",
                    exception);
            }
            catch (TaskCanceledException exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException(
                    "Voice transcription timed out.",
                    exception);
            }

            using (response)
            {
                var responseBody =
                    await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Voice transcription failed with HTTP {(int)response.StatusCode}.");
                }

                JsonDocument envelope;
                try
                {
                    envelope = JsonDocument.Parse(responseBody);
                }
                catch (JsonException exception)
                {
                    throw new InvalidOperationException(
                        "Voice transcription returned an invalid response.",
                        exception);
                }

                using (envelope)
                {
                    if (!envelope.RootElement.TryGetProperty("text", out var text))
                    {
                        throw new InvalidOperationException(
                            "Voice transcription returned an invalid response.");
                    }

                    cached.Text = text.GetString()?.Trim() ?? string.Empty;
                    return cached.Text;
                }
            }
        }
        finally
        {
            cached.Gate.Release();
        }
    }

    private Uri CreateFoundryEndpoint(string relativePath)
    {
        if (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(endpoint.Query) ||
            !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new InvalidOperationException(
                "AZURE_OPENAI_ENDPOINT must be an absolute resource URI without a path, query, or fragment.");
        }

        return new Uri(
            $"{endpoint.GetLeftPart(UriPartial.Authority)}/{relativePath}",
            UriKind.Absolute);
    }

    private async Task<string> SendChatCompletionAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> messages,
        bool useJsonResponse,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            CreateFoundryEndpoint(
                $"openai/deployments/{Uri.EscapeDataString(_options.ChatDeployment)}/chat/completions" +
                $"?api-version={ChatApiVersion}"));
        ApplyAuthentication(request);

        var foundryMessages = new List<object>
        {
            new
            {
                role = "system",
                content = systemPrompt,
            },
        };
        foreach (var message in messages)
        {
            var text = GetMessageText(message);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            foundryMessages.Add(new
            {
                role = ToFoundryRole(message.Role),
                content = text,
            });
        }

        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.ChatDeployment,
            ["max_completion_tokens"] = useJsonResponse ? 800 : 1_200,
            ["reasoning_effort"] = "minimal",
            ["messages"] = foundryMessages,
        };
        if (useJsonResponse)
        {
            payload["response_format"] = new
            {
                type = "json_object",
            };
        }
        request.Content = JsonContent.Create(payload);

        HttpResponseMessage response;
        try
        {
            response = await s_httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new InvalidOperationException(
                "The claim assistant could not reach Microsoft Foundry.",
                exception);
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                "The claim assistant timed out while waiting for Microsoft Foundry.",
                exception);
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"The claim assistant failed with HTTP {(int)response.StatusCode}.");
            }

            using var envelope = JsonDocument.Parse(responseBody);
            var choice = envelope.RootElement.GetProperty("choices")[0];
            var content = choice
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            if (string.IsNullOrWhiteSpace(content))
            {
                var finishReason = choice.TryGetProperty(
                    "finish_reason",
                    out var finishReasonElement)
                    ? finishReasonElement.GetString()
                    : null;
                var finishReasonSuffix = string.IsNullOrWhiteSpace(finishReason)
                    ? "."
                    : $" with finish reason '{finishReason}'.";
                throw new InvalidOperationException(
                    $"Microsoft Foundry returned an empty claim response{finishReasonSuffix}");
            }

            return content.Trim();
        }
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Configure AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_API_KEY before using the claim assistant.");
        }
    }

    private static string GetMessageText(ChatMessage message)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            parts.Add(message.Text);
        }

        parts.AddRange(message.Contents
            .OfType<DataContent>()
            .Select(content =>
                $"[Attached {content.MediaType}: {content.Name ?? "unnamed evidence"}]"));
        return string.Join(Environment.NewLine, parts);
    }

    private static string ToFoundryRole(ChatRole role)
        => role == ChatRole.Assistant ? "assistant" : "user";

    private void ApplyAuthentication(HttpRequestMessage request)
    {
        if (request.RequestUri?.Host.EndsWith(".azure.com", StringComparison.OrdinalIgnoreCase) is true)
        {
            request.Headers.Add("api-key", _options.ApiKey);
        }
        else
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
    }

    private sealed class CachedTranscript
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);

        public string? Text { get; set; }
    }

    private static string GetResponseOutputText(JsonElement root)
    {
        foreach (var item in root.GetProperty("output").EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var itemType) ||
                itemType.GetString() != "message" ||
                !item.TryGetProperty("content", out var content))
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var partType) &&
                    partType.GetString() == "output_text" &&
                    part.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? string.Empty;
                }
            }
        }

        throw new InvalidOperationException("Web research returned no text.");
    }

    private static List<ClaimResearchSource> GetResearchSources(
        JsonElement root,
        ClaimMarketResearch research)
    {
        var sources = new List<ClaimResearchSource>();
        foreach (var item in root.GetProperty("output").EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content))
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (!part.TryGetProperty("annotations", out var annotations))
                {
                    continue;
                }

                foreach (var annotation in annotations.EnumerateArray())
                {
                    if (annotation.TryGetProperty("type", out var type) &&
                        type.GetString() == "url_citation" &&
                        annotation.TryGetProperty("url", out var url) &&
                        ClaimResearchLink.Normalize(url.GetString()) is { } safeUrl)
                    {
                        sources.Add(new()
                        {
                            Title = annotation.TryGetProperty("title", out var title)
                                ? title.GetString() ?? safeUrl
                                : safeUrl,
                            Url = safeUrl,
                        });
                    }
                }
            }
        }

        sources.AddRange(research.ReplacementParts
            .Where(part => ClaimResearchLink.Normalize(part.SourceUrl) is not null)
            .Select(part => new ClaimResearchSource
            {
                Title = part.SourceTitle,
                Url = part.SourceUrl,
            }));

        return sources
            .DistinctBy(source => source.Url, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ExtractJsonObject(string value)
    {
        var firstBrace = value.IndexOf('{');
        var lastBrace = value.LastIndexOf('}');
        if (firstBrace < 0 || lastBrace <= firstBrace)
        {
            throw new InvalidOperationException("The model did not return a JSON object.");
        }

        return value[firstBrace..(lastBrace + 1)];
    }

    private sealed class ClaimEvidenceDecision
    {
        public bool AnalyzeEvidence { get; set; }
    }
}
