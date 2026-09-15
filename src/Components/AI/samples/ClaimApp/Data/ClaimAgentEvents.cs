// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace ComponentsAIClaimApp.Data;

internal abstract record ClaimAgentEvent;

internal sealed record ClaimAgentErrorEvent(string Code, string Message) : ClaimAgentEvent;

internal sealed record ClaimAgentTextEvent(
    string MessageId,
    string Delta) : ClaimAgentEvent;

internal sealed record ClaimAgentToolCallEvent(
    string MessageId,
    string ToolCallId,
    string ToolName,
    JsonElement Arguments) : ClaimAgentEvent;

internal sealed record ClaimAgentToolResultEvent(
    string ToolCallId,
    object? Result) : ClaimAgentEvent;

internal sealed record ClaimAgentApprovalRequestEvent(
    string MessageId,
    string RequestId,
    string ToolCallId,
    string ToolName,
    JsonElement Arguments) : ClaimAgentEvent;

internal sealed record ClaimAgentStateSnapshotEvent(JsonElement Snapshot) : ClaimAgentEvent;

internal sealed record ClaimAgentStateDeltaEvent(JsonElement Delta) : ClaimAgentEvent;
