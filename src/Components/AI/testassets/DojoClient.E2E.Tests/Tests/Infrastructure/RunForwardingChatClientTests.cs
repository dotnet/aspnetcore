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

namespace DojoClient.E2E.Tests.Tests.Infrastructure;

[TestClass]
[TestCategory("Infrastructure")]
public class RunForwardingChatClientTests
{
    private const string RunId = "11111111111111111111111111111111";

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task DirectRequestConstruction_ForwardsSessionWithoutMutatingCallerOptions(bool hasOptions)
    {
        var inner = new CapturingChatClient();
        var navigation = new TestNavigationManager();
        using var client = new RunForwardingChatClient(inner, navigation, DojoBackendKind.Direct);
        var state = new DojoRequestContext { ThreadId = "direct-thread" };
        var metadata = new AdditionalPropertiesDictionary
        {
            ["existing"] = 42,
            [DojoRunStore.RunKey] = "caller-owned",
            [DojoRequestContext.PropertyName] = state,
        };
        Func<IChatClient, object?> factory = _ => state;
        var options = hasOptions ? new ChatOptions
        {
            AdditionalProperties = metadata,
            RawRepresentationFactory = factory,
            Tools = [AIFunctionFactory.Create(() => "unused", name: "example_tool")],
        } : null;
        var messages = new[] { new ChatMessage(ChatRole.User, "Direct request") };
        using var cancellation = new CancellationTokenSource();

        var response = await client.GetResponseAsync(messages, options, cancellation.Token);

        Assert.AreEqual("Reply", response.Text);
        Assert.AreSame(messages, inner.Messages);
        Assert.AreEqual(cancellation.Token, inner.CancellationToken);
        Assert.IsNotNull(inner.Options);
        Assert.AreEqual(RunId, inner.Options.AdditionalProperties![DojoRunStore.RunKey]);
        if (hasOptions)
        {
            Assert.AreNotSame(options, inner.Options);
            Assert.AreNotSame(metadata, inner.Options.AdditionalProperties);
            Assert.AreEqual(42, inner.Options.AdditionalProperties["existing"]);
            Assert.AreSame(state, inner.Options.AdditionalProperties[DojoRequestContext.PropertyName]);
            Assert.AreSame(factory, inner.Options.RawRepresentationFactory);
            CollectionAssert.AreEqual(options!.Tools!.ToArray(), inner.Options.Tools!.ToArray());
            Assert.AreEqual("caller-owned", metadata[DojoRunStore.RunKey]);
        }
        else
        {
            Assert.HasCount(1, inner.Options.AdditionalProperties);
        }
    }

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

    private sealed class CapturingChatClient : IChatClient
    {
        internal IEnumerable<ChatMessage>? Messages { get; private set; }
        internal ChatOptions? Options { get; private set; }
        internal CancellationToken CancellationToken { get; private set; }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Messages = messages;
            Options = options;
            CancellationToken = cancellationToken;
            yield return new ChatResponseUpdate(ChatRole.Assistant, "Reply");
            await Task.CompletedTask;
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default)
            => GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose()
        {
        }
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
