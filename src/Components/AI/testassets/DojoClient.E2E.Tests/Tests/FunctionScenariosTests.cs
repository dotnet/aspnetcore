// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoAgent;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

[TestClass]
public class FunctionScenariosTests
{
    [TestMethod]
    public async Task InformationalUpdates_RemainInformationalAfterTheStreamAdvances()
    {
        var state = new FunctionScenarioState();
        using var client = FunctionScenarios.Create(state, requiresApproval: false);
        const string threadId = "retained-updates";
        var options = new ChatOptions
        {
            AdditionalProperties = new()
            {
                [DojoRequestContext.PropertyName] = new DojoRequestContext { ThreadId = threadId },
            },
        };
        var updates = new List<ChatResponseUpdate>();
        FunctionCallContent? publishedCall = null;
        await foreach (var update in client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "Show the weather")], options))
        {
            updates.Add(update);
            if (update.Contents.OfType<FunctionCallContent>().SingleOrDefault() is { } call)
            {
                publishedCall = call;
                Assert.IsTrue(call.InformationalOnly);
                state.ReleaseResult(threadId);
            }
        }

        var retainedCall = updates.SelectMany(update => update.Contents).OfType<FunctionCallContent>().Single();
        Assert.AreSame(publishedCall, retainedCall, "A published content list must not be rewritten after yielding.");
        Assert.IsTrue(retainedCall.InformationalOnly, "Published updates must not change when enumeration advances.");
        Assert.AreEqual(1, state.GetInvocationCount(threadId));
        Assert.AreEqual("sunny",
            updates.SelectMany(update => update.Contents).OfType<FunctionResultContent>().Single().Result?.ToString());
    }
}
