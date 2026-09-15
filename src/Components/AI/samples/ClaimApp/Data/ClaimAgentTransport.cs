// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace ComponentsAIClaimApp.Data;

internal sealed class ClaimAgentTransport
{
    private const string IdentifyToolCallId = "identify-vehicle-areas";
    private const string DisplayToolCallId = "display-vehicle-damage";
    private const string ApprovalToolCallId = "submit-claim-assessment";
    private const string AdditionalEvidenceReason = "Additional evidence requested";
    private const string BackendErrorMessage =
        "The claim assistant could not complete the request.";
    private readonly IClaimAssistantBackend _backend;
    private readonly ILogger<ClaimAgentTransport> _logger;

    public ClaimAgentTransport(
        IClaimAssistantBackend backend,
        ILogger<ClaimAgentTransport> logger)
    {
        _backend = backend;
        _logger = logger;
    }

    public async IAsyncEnumerable<ClaimAgentEvent> SendAsync(
        IReadOnlyList<ChatMessage> messages,
        JsonElement? stateSnapshot,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var lastMessage = messages.LastOrDefault();
        if (lastMessage?.Contents.OfType<ToolApprovalResponseContent>().LastOrDefault() is { } approval)
        {
            await foreach (var evt in CompleteApprovalAsync(
                messages,
                stateSnapshot,
                approval,
                cancellationToken))
            {
                yield return evt;
            }

            yield break;
        }

        if (lastMessage?.Contents.OfType<FunctionResultContent>().LastOrDefault() is { } toolResult)
        {
            await foreach (var evt in ContinueAfterToolAsync(
                messages,
                stateSnapshot,
                toolResult,
                cancellationToken))
            {
                yield return evt;
            }

            yield break;
        }

        var evidence = messages
            .Where(message => message.Role == ChatRole.User)
            .SelectMany(message => message.Contents)
            .OfType<DataContent>()
            .ToList();
        var media = evidence
            .Where(content => content.HasTopLevelMediaType("image") ||
                content.HasTopLevelMediaType("audio"))
            .ToList();
        var currentMedia = lastMessage?.Contents
            .OfType<DataContent>()
            .Where(content => content.HasTopLevelMediaType("image") ||
                content.HasTopLevelMediaType("audio"))
            .ToList() ?? [];
        var images = media
            .Where(content => content.HasTopLevelMediaType("image"))
            .ToList();
        var audio = media
            .Where(content => content.HasTopLevelMediaType("audio"))
            .ToList();
        if (images.Count > ClaimLimits.MaximumPhotoCount)
        {
            yield return new ClaimAgentErrorEvent(
                "claim_evidence_limit",
                "A claim can include up to six photos.");
            yield break;
        }
        if (images.Any(image =>
            image.Data.Length > ClaimLimits.MaximumPhotoBytes))
        {
            yield return new ClaimAgentErrorEvent(
                "claim_evidence_limit",
                "Each claim photo must be 8 MB or smaller.");
            yield break;
        }
        if (evidence.Sum(content => (long)content.Data.Length) >
            ClaimLimits.MaximumEvidenceBytes)
        {
            yield return new ClaimAgentErrorEvent(
                "claim_evidence_limit",
                "A claim can include up to 24 MB of total evidence.");
            yield break;
        }

        var state = ReadState(stateSnapshot);
        var routing = await CallBackendAsync(
            () => _backend.ShouldAnalyzeEvidenceAsync(
                messages,
                state,
                currentMedia.Count,
                cancellationToken));
        if (routing.Error is not null)
        {
            yield return new ClaimAgentErrorEvent(
                "claim_conversation_failure",
                routing.Error);
            yield break;
        }

        if (!routing.Value)
        {
            if (state.Confidence == 0)
            {
                state.Status = "Ready for claim details";
            }
            yield return new ClaimAgentStateSnapshotEvent(
                JsonSerializer.SerializeToElement(state, ClaimStateJson.Options));

            var response = await CallBackendAsync(
                () => _backend.GenerateResponseAsync(
                    messages,
                    state,
                    ClaimResponsePurpose.Conversation,
                    cancellationToken));
            if (response.Error is not null)
            {
                yield return new ClaimAgentErrorEvent(
                    "claim_conversation_failure",
                    response.Error);
                yield break;
            }
            yield return TextMessage(response.Value);
            yield break;
        }

        var currentUserText =
            lastMessage?.Text ?? "Review the attached claim evidence.";
        var userText = string.Join(
            Environment.NewLine,
            messages
                .Where(message => message.Role == ChatRole.User)
                .Select(message => message.Text)
                .Where(text =>
                    !string.IsNullOrWhiteSpace(text) &&
                    !string.Equals(
                        text,
                        "Review the attached claim evidence.",
                        StringComparison.OrdinalIgnoreCase)));
        if (string.IsNullOrWhiteSpace(userText))
        {
            userText = currentUserText;
        }

        state.Status = "Analyzing evidence with Foundry";
        state.AccidentSummary = userText;
        state.EvidenceSummary = DescribeMedia(media);
        state.Decision = "Pending";
        state.RejectionReason = null;

        yield return new ClaimAgentStateSnapshotEvent(
            JsonSerializer.SerializeToElement(state, ClaimStateJson.Options));

        var analysis = await CallBackendAsync(
            () => _backend.AnalyzeAsync(
                userText,
                images,
                audio,
                cancellationToken));
        if (analysis.Error is not null)
        {
            yield return new ClaimAgentErrorEvent(
                "vision_analysis_failure",
                analysis.Error);
            yield break;
        }

        var completedAnalysis = analysis.Value;
        state.Status = "Evidence analyzed";
        state.AssessmentSummary = completedAnalysis.Summary;
        state.AffectedAreas = [];
        state.Confidence = completedAnalysis.Confidence;
        state.Decision = "Pending";
        state.RejectionReason = null;
        state.DamageFindings = completedAnalysis.Findings;
        state.NextPhotoSuggestion = completedAnalysis.NextPhotoSuggestion;
        state.NeedsHumanReview = completedAnalysis.NeedsHumanReview;
        state.VoiceTranscript = completedAnalysis.VoiceTranscript;
        state.RepairEstimate = completedAnalysis.RepairEstimate;
        state.ReplacementParts = completedAnalysis.ReplacementParts;
        state.ResearchSources = completedAnalysis.ResearchSources;
        state.ResearchWarning = completedAnalysis.ResearchWarning;

        yield return new ClaimAgentStateSnapshotEvent(
            JsonSerializer.SerializeToElement(state, ClaimStateJson.Options));

        if (!HasReviewableAssessment(completedAnalysis))
        {
            state.Status = "More evidence needed";
            state.Decision = "Pending";
            yield return new ClaimAgentStateSnapshotEvent(
                JsonSerializer.SerializeToElement(state, ClaimStateJson.Options));

            var response = await CallBackendAsync(
                () => _backend.GenerateResponseAsync(
                    messages,
                    state,
                    ClaimResponsePurpose.MoreEvidence,
                    cancellationToken));
            if (response.Error is not null)
            {
                yield return new ClaimAgentErrorEvent(
                    "claim_conversation_failure",
                    response.Error);
                yield break;
            }
            yield return TextMessage(response.Value);
            yield break;
        }

        var identifyToolCallId = CreateToolCallId(IdentifyToolCallId);
        yield return ToolCall(
            identifyToolCallId,
            "identify_vehicle_areas",
            new { description = userText });
        yield return new ClaimAgentToolResultEvent(
            identifyToolCallId,
            new VehicleAreaAssessment
            {
                Areas = completedAnalysis.AffectedAreas,
                Severity = completedAnalysis.Findings
                    .Select(finding => finding.Severity)
                    .FirstOrDefault() ?? "Moderate",
            });
        yield return ToolCall(
            CreateToolCallId(DisplayToolCallId),
            "display_vehicle_damage",
            new { areas = completedAnalysis.AffectedAreas });
    }

    private async IAsyncEnumerable<ClaimAgentEvent> ContinueAfterToolAsync(
        IReadOnlyList<ChatMessage> messages,
        JsonElement? stateSnapshot,
        FunctionResultContent toolResult,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new ClaimAgentToolResultEvent(toolResult.CallId, toolResult.Result);

        if (!IsToolCallId(toolResult.CallId, DisplayToolCallId))
        {
            yield break;
        }

        var state = ReadState(stateSnapshot);
        state.Status = "Awaiting your decision";
        state.Decision = "Pending";

        yield return new ClaimAgentStateDeltaEvent(
            JsonSerializer.SerializeToElement(new object[]
            {
                new { op = "replace", path = "/status", value = state.Status },
                new { op = "replace", path = "/assessmentSummary", value = state.AssessmentSummary },
                new { op = "replace", path = "/confidence", value = state.Confidence },
                new { op = "replace", path = "/decision", value = state.Decision },
                new { op = "replace", path = "/damageFindings", value = state.DamageFindings },
                new { op = "replace", path = "/nextPhotoSuggestion", value = state.NextPhotoSuggestion },
                new { op = "replace", path = "/needsHumanReview", value = state.NeedsHumanReview },
                new { op = "replace", path = "/voiceTranscript", value = state.VoiceTranscript },
                new { op = "replace", path = "/repairEstimate", value = state.RepairEstimate },
                new { op = "replace", path = "/replacementParts", value = state.ReplacementParts },
                new { op = "replace", path = "/researchSources", value = state.ResearchSources },
                new { op = "replace", path = "/researchWarning", value = state.ResearchWarning },
            }));

        var response = await CallBackendAsync(
            () => _backend.GenerateResponseAsync(
                messages,
                state,
                ClaimResponsePurpose.AssessmentReady,
                cancellationToken));
        if (response.Error is not null)
        {
            yield return new ClaimAgentErrorEvent(
                "claim_conversation_failure",
                response.Error);
            yield break;
        }

        yield return TextMessage(response.Value);
        var approvalToolCallId = CreateToolCallId(ApprovalToolCallId);
        yield return new ClaimAgentApprovalRequestEvent(
            Guid.NewGuid().ToString("N"),
            $"approve-{approvalToolCallId}",
            approvalToolCallId,
            "submit_claim_assessment",
            JsonSerializer.SerializeToElement(new
            {
                state.AssessmentSummary,
                state.Confidence,
            }, ClaimStateJson.Options));
    }

    private async IAsyncEnumerable<ClaimAgentEvent> CompleteApprovalAsync(
        IReadOnlyList<ChatMessage> messages,
        JsonElement? stateSnapshot,
        ToolApprovalResponseContent approval,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var state = ReadState(stateSnapshot);
        var requestsAdditionalEvidence =
            !approval.Approved &&
            (string.Equals(
                    approval.Reason,
                    AdditionalEvidenceReason,
                    StringComparison.Ordinal) ||
                string.Equals(
                    state.Status,
                    "Add more evidence",
                    StringComparison.Ordinal));
        state.Decision = requestsAdditionalEvidence
            ? "Pending"
            : approval.Approved ? "Approved" : "Rejected";
        state.Status = requestsAdditionalEvidence
            ? "Add more evidence"
            : approval.Approved ? "Assessment approved" : "Assessment rejected";
        state.RejectionReason = requestsAdditionalEvidence
            ? null
            : approval.Reason ?? state.RejectionReason;

        yield return new ClaimAgentToolResultEvent(
            approval.ToolCall.CallId ?? ApprovalToolCallId,
            requestsAdditionalEvidence
                ? "The user requested another evidence collection turn."
                : approval.Approved
                ? "The user approved the assessment."
                : $"The user rejected the assessment. Reason: {state.RejectionReason ?? "No reason supplied."}");

        yield return new ClaimAgentStateDeltaEvent(
            JsonSerializer.SerializeToElement(new object[]
            {
                new { op = "replace", path = "/status", value = state.Status },
                new { op = "replace", path = "/decision", value = state.Decision },
                new { op = "replace", path = "/rejectionReason", value = state.RejectionReason },
            }));

        var response = await CallBackendAsync(
            () => _backend.GenerateResponseAsync(
                messages,
                state,
                ClaimResponsePurpose.Decision,
                cancellationToken));
        if (response.Error is not null)
        {
            yield return new ClaimAgentErrorEvent(
                "claim_conversation_failure",
                response.Error);
            yield break;
        }

        yield return TextMessage(response.Value);
    }

    private async Task<BackendResult<T>> CallBackendAsync<T>(
        Func<Task<T>> operation)
    {
        try
        {
            return new(await operation(), null);
        }
        catch (HttpRequestException exception)
        {
            return BackendFailure<T>(exception);
        }
        catch (InvalidOperationException exception)
        {
            return BackendFailure<T>(exception);
        }
        catch (JsonException exception)
        {
            return BackendFailure<T>(exception);
        }
    }

    private BackendResult<T> BackendFailure<T>(Exception exception)
    {
        _logger.LogError(exception, "The claim assistant backend request failed.");
        return new(default!, BackendErrorMessage);
    }

    private static ClaimAgentTextEvent TextMessage(string text)
        => new(Guid.NewGuid().ToString("N"), text);

    private static ClaimAgentToolCallEvent ToolCall(
        string toolCallId,
        string toolName,
        object arguments)
        => new(
            Guid.NewGuid().ToString("N"),
            toolCallId,
            toolName,
            JsonSerializer.SerializeToElement(arguments, ClaimStateJson.Options));

    private static string CreateToolCallId(string prefix)
        => $"{prefix}-{Guid.NewGuid():N}";

    private static bool IsToolCallId(string? callId, string prefix)
        => string.Equals(callId, prefix, StringComparison.Ordinal) ||
            callId?.StartsWith($"{prefix}-", StringComparison.Ordinal) == true;

    private static ClaimState ReadState(JsonElement? state)
        => state?.Deserialize<ClaimState>(ClaimStateJson.Options) ?? new ClaimState();

    private static string DescribeMedia(IReadOnlyList<DataContent> media)
    {
        var imageCount = media.Count(content => content.HasTopLevelMediaType("image"));
        var audioCount = media.Count(content => content.HasTopLevelMediaType("audio"));
        var parts = new List<string>();
        if (imageCount > 0)
        {
            parts.Add($"{imageCount} image{(imageCount == 1 ? string.Empty : "s")}");
        }
        if (audioCount > 0)
        {
            parts.Add($"{audioCount} voice note{(audioCount == 1 ? string.Empty : "s")}");
        }

        return parts.Count == 0 ? "No evidence attached." : string.Join(" and ", parts);
    }

    private static bool HasReviewableAssessment(ClaimDamageAnalysis analysis)
        => analysis.Confidence >= 25 &&
            (analysis.Findings.Count > 0 || analysis.AffectedAreas.Count > 0);

    private readonly record struct BackendResult<T>(T Value, string? Error);
}
