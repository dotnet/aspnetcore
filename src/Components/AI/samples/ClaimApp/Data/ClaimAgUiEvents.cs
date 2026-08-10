// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.AI;

namespace ComponentsAIClaimApp.Data;

internal sealed class ClaimAgUiRunInput
{
    public IReadOnlyList<ChatMessage> Messages { get; set; } = [];

    public JsonElement? State { get; init; }
}

internal abstract record ClaimAgUiEvent;

internal sealed record ClaimAgUiRunStartedEvent(string RunId) : ClaimAgUiEvent;

internal sealed record ClaimAgUiRunFinishedEvent(string RunId) : ClaimAgUiEvent;

internal sealed record ClaimAgUiRunErrorEvent(string Code, string Message) : ClaimAgUiEvent;

internal sealed record ClaimAgUiTextMessageContentEvent(
    string MessageId,
    string Delta) : ClaimAgUiEvent;

internal sealed record ClaimAgUiToolCallEvent(
    string ToolCallId,
    string ToolName,
    JsonElement Arguments) : ClaimAgUiEvent;

internal sealed record ClaimAgUiToolResultEvent(
    string ToolCallId,
    object? Result) : ClaimAgUiEvent;

internal sealed record ClaimAgUiApprovalRequestEvent(
    string RequestId,
    string ToolCallId,
    string ToolName,
    JsonElement Arguments) : ClaimAgUiEvent;

internal sealed record ClaimAgUiStateSnapshotEvent(JsonElement Snapshot) : ClaimAgUiEvent;

internal sealed record ClaimAgUiStateDeltaEvent(JsonElement Delta) : ClaimAgUiEvent;
