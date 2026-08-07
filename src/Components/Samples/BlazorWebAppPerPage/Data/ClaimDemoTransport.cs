// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace BlazorWebAppPerPage.Data;

internal sealed class ClaimDemoTransport
{
    private const string IdentifyToolCallId = "identify-vehicle-areas";
    private const string DisplayToolCallId = "display-vehicle-damage";
    private const string ApprovalToolCallId = "submit-claim-assessment";
    private const string ApprovalRequestId = "approve-claim-assessment";

    public async IAsyncEnumerable<ClaimAgUiEvent> SendAsync(
        ClaimAgUiRunInput input,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid().ToString("N");
        yield return new ClaimAgUiRunStartedEvent(runId);

        var lastMessage = input.Messages.LastOrDefault();
        if (lastMessage?.Contents.OfType<ToolApprovalResponseContent>().LastOrDefault() is { } approval)
        {
            await foreach (var evt in CompleteApprovalAsync(input, approval, cancellationToken))
            {
                yield return evt;
            }

            yield return new ClaimAgUiRunFinishedEvent(runId);
            yield break;
        }

        if (lastMessage?.Contents.OfType<FunctionResultContent>().LastOrDefault() is { } toolResult)
        {
            await foreach (var evt in ContinueAfterToolAsync(input, toolResult, cancellationToken))
            {
                yield return evt;
            }

            yield return new ClaimAgUiRunFinishedEvent(runId);
            yield break;
        }

        var userText = lastMessage?.Text ?? "The front of my car was hit in a parking lot.";
        if (userText.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            await Task.Delay(450, cancellationToken);
            yield return new ClaimAgUiRunErrorEvent(
                "demo_assessment_failure",
                "The demo AG-UI damage model failed while reading the accident description.");
            yield break;
        }

        var state = ReadState(input);
        state.Status = "Inspecting accident details";
        state.AccidentSummary = userText;
        state.AssessmentSummary = "Identifying likely affected vehicle areas.";
        state.AffectedAreas = [];
        state.Confidence = 0;
        state.Decision = "Pending";
        state.RejectionReason = null;

        yield return new ClaimAgUiStateSnapshotEvent(
            JsonSerializer.SerializeToElement(state, ClaimStateJson.Options));

        await foreach (var evt in StreamTextAsync(
            "assessment-start",
            ["I am reviewing the accident description. ", "Next I will identify the likely impact areas."],
            cancellationToken))
        {
            yield return evt;
        }

        await Task.Delay(700, cancellationToken);
        yield return ToolCall(
            IdentifyToolCallId,
            "identify_vehicle_areas",
            new { description = userText });
        yield return new ClaimAgUiRunFinishedEvent(runId);
    }

    private static async IAsyncEnumerable<ClaimAgUiEvent> ContinueAfterToolAsync(
        ClaimAgUiRunInput input,
        FunctionResultContent toolResult,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new ClaimAgUiToolResultEvent(toolResult.CallId, toolResult.Result);

        if (toolResult.CallId == IdentifyToolCallId)
        {
            var assessment = ReadAssessment(toolResult.Result);
            await Task.Delay(350, cancellationToken);
            yield return ToolCall(
                DisplayToolCallId,
                "display_vehicle_damage",
                new { areas = assessment.Areas });
            yield break;
        }

        if (toolResult.CallId != DisplayToolCallId)
        {
            yield break;
        }

        var state = ReadState(input);
        state.Status = "Awaiting your decision";
        state.AssessmentSummary =
            $"Likely {string.Join(", ", state.AffectedAreas.Select(ToDisplayName))} damage. " +
            "A repair facility should confirm the final scope.";
        state.Confidence = 86;
        state.Decision = "Pending";

        yield return new ClaimAgUiStateDeltaEvent(
            JsonSerializer.SerializeToElement(new object[]
            {
                new { op = "replace", path = "/status", value = state.Status },
                new { op = "replace", path = "/assessmentSummary", value = state.AssessmentSummary },
                new { op = "replace", path = "/confidence", value = state.Confidence },
                new { op = "replace", path = "/decision", value = state.Decision },
            }));

        await foreach (var evt in StreamTextAsync(
            "assessment-summary",
            [
                "The impact pattern suggests damage to ",
                $"{string.Join(", ", state.AffectedAreas.Select(ToDisplayName))}. ",
                "I estimate moderate damage with 86% confidence. Please approve or reject this assessment.",
            ],
            cancellationToken))
        {
            yield return evt;
        }

        yield return new ClaimAgUiApprovalRequestEvent(
            ApprovalRequestId,
            ApprovalToolCallId,
            "submit_claim_assessment",
            JsonSerializer.SerializeToElement(new
            {
                state.AssessmentSummary,
                state.Confidence,
            }, ClaimStateJson.Options));
    }

    private static async IAsyncEnumerable<ClaimAgUiEvent> CompleteApprovalAsync(
        ClaimAgUiRunInput input,
        ToolApprovalResponseContent approval,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var state = ReadState(input);
        state.Decision = approval.Approved ? "Approved" : "Rejected";
        state.Status = approval.Approved ? "Assessment approved" : "Assessment rejected";
        state.RejectionReason = approval.Reason ?? state.RejectionReason;

        yield return new ClaimAgUiToolResultEvent(
            ApprovalToolCallId,
            approval.Approved
                ? "The user approved the assessment."
                : $"The user rejected the assessment. Reason: {state.RejectionReason ?? "No reason supplied."}");

        yield return new ClaimAgUiStateDeltaEvent(
            JsonSerializer.SerializeToElement(new object[]
            {
                new { op = "replace", path = "/status", value = state.Status },
                new { op = "replace", path = "/decision", value = state.Decision },
                new { op = "replace", path = "/rejectionReason", value = state.RejectionReason },
            }));

        await foreach (var evt in StreamTextAsync(
            "assessment-decision-message",
            approval.Approved
                ? ["The assessment is approved and ready for the next claim step."]
                : [$"The assessment was rejected. I recorded the reason: {state.RejectionReason ?? "No reason supplied."}"],
            cancellationToken))
        {
            yield return evt;
        }
    }

    private static async IAsyncEnumerable<ClaimAgUiEvent> StreamTextAsync(
        string messageId,
        IReadOnlyList<string> chunks,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var chunk in chunks)
        {
            await Task.Delay(450, cancellationToken);
            yield return new ClaimAgUiTextMessageContentEvent(messageId, chunk);
        }
    }

    private static ClaimAgUiToolCallEvent ToolCall(
        string toolCallId,
        string toolName,
        object arguments)
        => new(
            toolCallId,
            toolName,
            JsonSerializer.SerializeToElement(arguments, ClaimStateJson.Options));

    private static ClaimState ReadState(ClaimAgUiRunInput input)
        => input.State?.Deserialize<ClaimState>(ClaimStateJson.Options) ?? new ClaimState();

    private static VehicleAreaAssessment ReadAssessment(object? result)
    {
        if (result is VehicleAreaAssessment assessment)
        {
            return assessment;
        }

        if (result is JsonElement element)
        {
            return element.Deserialize<VehicleAreaAssessment>(ClaimStateJson.Options)
                ?? DefaultAssessment();
        }

        if (result is string json)
        {
            try
            {
                return JsonSerializer.Deserialize<VehicleAreaAssessment>(json, ClaimStateJson.Options)
                    ?? DefaultAssessment();
            }
            catch (JsonException)
            {
            }
        }

        return DefaultAssessment();
    }

    private static VehicleAreaAssessment DefaultAssessment()
        => new()
        {
            Areas = ["front-bumper", "hood", "left-fender"],
            Severity = "Moderate",
        };

    private static string ToDisplayName(string area)
        => area.Replace('-', ' ');
}
