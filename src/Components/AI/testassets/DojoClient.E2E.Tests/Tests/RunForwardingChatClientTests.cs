// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using AGUI.Abstractions;
using AGUI.Client;
using DojoAgent;
using DojoClient.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.AI;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

[TestClass]
public class RunForwardingChatClientTests
{
    private const string RunId = "11111111111111111111111111111111";

    [TestMethod]
    [DataRow(false, false)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public async Task AguiRequestConstruction_PreservesDefaultsAndExplicitMetadata(
        bool hasOptions, bool explicitMetadata)
    {
        var baseline = await CaptureRequestsAsync(forwardRun: false, hasOptions, explicitMetadata);
        var forwarded = await CaptureRequestsAsync(forwardRun: true, hasOptions, explicitMetadata);
        Assert.AreEqual(2, forwarded.Count);
        Assert.AreNotEqual(forwarded[0].RunId, forwarded[1].RunId);
        Assert.AreEqual(baseline[0].ThreadId == baseline[1].ThreadId,
            forwarded[0].ThreadId == forwarded[1].ThreadId);

        for (var index = 0; index < forwarded.Count; index++)
        {
            var input = forwarded[index];
            Assert.IsFalse(string.IsNullOrEmpty(input.ThreadId));
            Assert.IsFalse(string.IsNullOrEmpty(input.RunId));
            Assert.AreEqual(RunId, input.ForwardedProperties.GetProperty(DojoRunStore.RunKey).GetString());
            CollectionAssert.AreEqual(
                baseline[index].Messages.AsChatMessages().Select(message => (message.Role, message.Text)).ToArray(),
                input.Messages.AsChatMessages().Select(message => (message.Role, message.Text)).ToArray());
            Assert.AreEqual(index == 0 ? 1 : 3, input.Messages.Count);
            CollectionAssert.AreEqual(
                baseline[index].Tools?.Select(tool => tool.Name).ToArray() ?? [],
                input.Tools?.Select(tool => tool.Name).ToArray() ?? []);
            if (explicitMetadata)
            {
                Assert.AreEqual("explicit-thread", input.ThreadId);
                Assert.AreEqual(42, input.ForwardedProperties.GetProperty("existing").GetInt32());
                Assert.IsTrue(JsonElement.DeepEquals(baseline[index].State!.Value, input.State!.Value));
            }
        }
    }

    private static async Task<List<RunAgentInput>> CaptureRequestsAsync(
        bool forwardRun, bool hasOptions, bool explicitMetadata)
    {
        var transport = new CapturingTransport();
        IChatClient client = new AGUIChatClient(new AGUIChatClientOptions { Transport = transport });
        if (forwardRun)
        {
            client = new RunForwardingChatClient(client, new TestNavigationManager(), DojoBackendKind.AGUI);
        }

        using var agent = new UIAgent(client, options =>
        {
            if (!hasOptions)
            {
                return;
            }

            options.ChatOptions = new ChatOptions
            {
                Tools = [AIFunctionFactory.Create(() => "unused", name: "example_tool")],
            };
            if (explicitMetadata)
            {
                options.ChatOptions.RawRepresentationFactory = _ => new RunAgentInput
                {
                    ThreadId = "explicit-thread",
                    State = JsonSerializer.SerializeToElement(new { document = "draft" }),
                    ForwardedProperties = JsonSerializer.SerializeToElement(new { existing = 42 }),
                };
            }
        });
        await foreach (var _ in agent.SendMessageAsync(new ChatMessage(ChatRole.User, "First")))
        {
        }
        await foreach (var _ in agent.SendMessageAsync(new ChatMessage(ChatRole.User, "Second")))
        {
        }

        return transport.Requests;
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
            => Initialize("http://localhost/", $"http://localhost/agentic_chat?{DojoRunStore.RunKey}={RunId}");

        protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotSupportedException();
    }

    private sealed class CapturingTransport : IAGUITransport
    {
        internal List<RunAgentInput> Requests { get; } = [];

        public async IAsyncEnumerable<BaseEvent> SendAsync(
            RunAgentInput input,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Requests.Add(input);
            var messageId = $"reply-{Requests.Count}";
            yield return new RunStartedEvent { ThreadId = input.ThreadId, RunId = input.RunId };
            yield return new TextMessageStartEvent { MessageId = messageId, Role = "assistant" };
            yield return new TextMessageContentEvent { MessageId = messageId, Delta = "Reply" };
            yield return new TextMessageEndEvent { MessageId = messageId };
            yield return new RunFinishedEvent { ThreadId = input.ThreadId, RunId = input.RunId };
            await Task.CompletedTask;
        }
    }
}
