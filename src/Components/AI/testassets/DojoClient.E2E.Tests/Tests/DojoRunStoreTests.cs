// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoClient.E2E.Tests.ServiceOverrides;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

[TestClass]
public class DojoRunStoreTests
{
    [TestMethod]
    public async Task Disposal_FailsPromptlyForAnAbandonedEnumerator()
    {
        var run = new DojoRunStore.Run(new ConfigurationBuilder().Build(), DojoRecording.AgenticChat);
        var enumerator = run.GetUpdatesAsync(
            [new ChatMessage(ChatRole.User, "Tell me about Blazor")],
            options: null, predictive: false, CancellationToken.None).GetAsyncEnumerator();
        Assert.IsTrue(await enumerator.MoveNextAsync());
        var disposal = run.DisposeAsync(TimeSpan.FromMilliseconds(50)).AsTask();

        try
        {
            Assert.AreSame(disposal, await Task.WhenAny(disposal, Task.Delay(TimeSpan.FromSeconds(2))),
                "An abandoned stream must not block test cleanup indefinitely.");
            await Assert.ThrowsAsync<TimeoutException>(() => disposal);
        }
        finally
        {
            await enumerator.DisposeAsync();
            try
            {
                await disposal;
            }
            catch (TimeoutException)
            {
                await run.DisposeAsync();
            }
        }
    }
}
