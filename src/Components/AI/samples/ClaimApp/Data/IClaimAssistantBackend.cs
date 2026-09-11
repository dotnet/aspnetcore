// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace ComponentsAIClaimApp.Data;

public interface IClaimAssistantBackend
{
    string ModelName { get; }

    Task<bool> ShouldAnalyzeEvidenceAsync(
        IReadOnlyList<ChatMessage> messages,
        ClaimState state,
        int currentMediaCount,
        CancellationToken cancellationToken);

    Task<string> GenerateResponseAsync(
        IReadOnlyList<ChatMessage> messages,
        ClaimState state,
        ClaimResponsePurpose purpose,
        CancellationToken cancellationToken);

    Task<ClaimDamageAnalysis> AnalyzeAsync(
        string description,
        IReadOnlyList<DataContent> images,
        IReadOnlyList<DataContent> audio,
        CancellationToken cancellationToken);

    Task<string> TranscribeAsync(
        DataContent recording,
        CancellationToken cancellationToken);
}
