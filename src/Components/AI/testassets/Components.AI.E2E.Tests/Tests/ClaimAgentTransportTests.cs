// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ComponentsAIClaimApp.Data;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

[TestClass]
public class ClaimAgentTransportTests
{
    [TestMethod]
    public async Task SendAsync_RejectsPhotoCountIndependently()
    {
        var contents = Enumerable.Range(0, ClaimLimits.MaximumPhotoCount + 1)
            .Select(_ => new DataContent(new byte[] { 1 }, "image/jpeg"))
            .Cast<AIContent>()
            .ToList();

        var error = await SendForErrorAsync(contents);

        Assert.AreEqual("claim_evidence_limit", error.Code);
        Assert.AreEqual("A claim can include up to six photos.", error.Message);
    }

    [TestMethod]
    public async Task SendAsync_RejectsOversizedPhoto()
    {
        var contents = new AIContent[]
        {
            new DataContent(
                new byte[checked((int)ClaimLimits.MaximumPhotoBytes + 1)],
                "image/jpeg"),
        };

        var error = await SendForErrorAsync(contents);

        Assert.AreEqual("claim_evidence_limit", error.Code);
        Assert.AreEqual("Each claim photo must be 8 MB or smaller.", error.Message);
    }

    [TestMethod]
    public async Task SendAsync_CountsAllBinaryEvidenceTowardTotal()
    {
        var contents = new AIContent[]
        {
            new DataContent(
                new byte[checked((int)ClaimLimits.MaximumPhotoBytes)],
                "image/jpeg"),
            new DataContent(
                new byte[checked((int)ClaimLimits.MaximumPhotoBytes)],
                "audio/webm"),
            new DataContent(
                new byte[checked((int)ClaimLimits.MaximumPhotoBytes + 1)],
                "application/pdf"),
        };

        var error = await SendForErrorAsync(contents);

        Assert.AreEqual("claim_evidence_limit", error.Code);
        Assert.AreEqual(
            "A claim can include up to 24 MB of total evidence.",
            error.Message);
    }

    private static async Task<ClaimAgentErrorEvent> SendForErrorAsync(
        IReadOnlyList<AIContent> contents)
    {
        var transport = new ClaimAgentTransport(
            new UnexpectedBackend(),
            NullLogger<ClaimAgentTransport>.Instance);
        var events = new List<ClaimAgentEvent>();
        await foreach (var evt in transport.SendAsync(
            [new ChatMessage(ChatRole.User, [.. contents])],
            stateSnapshot: null,
            CancellationToken.None))
        {
            events.Add(evt);
        }

        Assert.HasCount(1, events);
        return Assert.IsInstanceOfType<ClaimAgentErrorEvent>(events[0]);
    }

    private sealed class UnexpectedBackend : IClaimAssistantBackend
    {
        public string ModelName => throw new InvalidOperationException();

        public Task<bool> ShouldAnalyzeEvidenceAsync(
            IReadOnlyList<ChatMessage> messages,
            ClaimState state,
            int currentMediaCount,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException();

        public Task<string> GenerateResponseAsync(
            IReadOnlyList<ChatMessage> messages,
            ClaimState state,
            ClaimResponsePurpose purpose,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException();

        public Task<ClaimDamageAnalysis> AnalyzeAsync(
            string description,
            IReadOnlyList<DataContent> images,
            IReadOnlyList<DataContent> audio,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException();

        public Task<string> TranscribeAsync(
            DataContent recording,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException();
    }
}
