// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Identity;
using ComponentsAIClaimApp.Data;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

[TestClass]
public class ClaimDamageAnalyzerTests
{
    [TestMethod]
    public async Task TranscribeAsync_NormalizesWrappedCredentialCancellation()
    {
        var credential = new CancellationWrappingCredential();
        var analyzer = new ClaimDamageAnalyzer(
            new ClaimFoundryOptions
            {
                Endpoint = "https://example.openai.azure.com",
            },
            credential);
        using var cancellationSource = new CancellationTokenSource();
        var transcriptionTask = analyzer.TranscribeAsync(
            new DataContent(new byte[] { 1, 2, 3, 4 }, "audio/webm"),
            cancellationSource.Token);

        await credential.AuthenticationStarted;
        cancellationSource.Cancel();

        var exception = await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            async () => await transcriptionTask);
        Assert.AreEqual(cancellationSource.Token, exception.CancellationToken);
    }

    private sealed class CancellationWrappingCredential : TokenCredential
    {
        private readonly TaskCompletionSource _authenticationStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task AuthenticationStarted => _authenticationStarted.Task;

        public override AccessToken GetToken(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override async ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            _authenticationStarted.SetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException();
            }
            catch (OperationCanceledException exception)
            {
                throw new AuthenticationFailedException(
                    "Authentication was canceled.",
                    exception);
            }
        }
    }
}
