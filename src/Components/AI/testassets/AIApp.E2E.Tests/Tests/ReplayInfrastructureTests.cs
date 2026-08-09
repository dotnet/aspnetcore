// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using AIApp.Components.Scenarios.AgenticGenerativeUI;
using AIApp.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.AI;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIApp.E2E.Tests.Tests;

[TestClass]
public class ReplayInfrastructureTests
{
    [TestMethod]
    public async Task CapturingChatClient_RecordsDecodedMessagesAndUpdates()
    {
        var fake = new FakeChatClient();
        fake.Enqueue((messages, _, cancellationToken) =>
        {
            Assert.AreEqual("hello", messages.Single().Text);
            return YieldAsync(
                [new ChatResponseUpdate(ChatRole.Assistant, "captured")],
                cancellationToken);
        });
        using var client = new CapturingChatClient(fake);

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")]);

        Assert.AreEqual("captured", response.Text);
        Assert.HasCount(1, client.Calls);
        Assert.AreEqual("hello", client.Calls[0].Messages.Single().Text);
        Assert.AreEqual("captured", client.Calls[0].Updates.Single().Text);
    }

    [TestMethod]
    public async Task GatedReplayChatClient_WaitsAfterNamedFrame()
    {
        var script = new ReplayCheckpointScript
        {
            Calls =
            [
                new ReplayCall
                {
                    Request = new ReplayRequestExpectation
                    {
                        LastUserMessage = "hello",
                        MessageCount = 1,
                    },
                    Frames =
                    [
                        new ReplayFrame
                        {
                            Name = "assistant-text",
                            Updates = [new ChatResponseUpdate(ChatRole.Assistant, "hello back")],
                        },
                    ],
                },
            ],
        };
        var locks = new TestLockProvider();
        var session = new TestSessionContext { Id = "session" };
        using var client = new GatedReplayChatClient(script, locks, session);
        await using var enumerator = client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")]).GetAsyncEnumerator();

        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.AreEqual("hello back", enumerator.Current.Text);

        var completion = enumerator.MoveNextAsync().AsTask();
        Assert.IsFalse(completion.IsCompleted);

        locks.Release($"session:{script.GetLockName(0, 0)}");

        Assert.IsFalse(await completion);
    }

    [TestMethod]
    public async Task GatedReplayChatClient_CancelsCheckpointWait()
    {
        var script = CreateSingleFrameScript();
        var locks = new TestLockProvider();
        var session = new TestSessionContext { Id = "cancel-session" };
        using var client = new GatedReplayChatClient(script, locks, session);
        using var cancellation = new CancellationTokenSource();
        await using var enumerator = client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")],
            cancellationToken: cancellation.Token).GetAsyncEnumerator(cancellation.Token);

        Assert.IsTrue(await enumerator.MoveNextAsync());
        var completion = enumerator.MoveNextAsync().AsTask();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await completion);
    }

    [TestMethod]
    public async Task GatedReplayChatClient_IsolatesSessionsAndCalls()
    {
        var script = new ReplayCheckpointScript
        {
            Calls = [CreateSingleFrameCall(), CreateSingleFrameCall()],
        };
        var locks = new TestLockProvider();
        using var firstClient = new GatedReplayChatClient(
            script,
            locks,
            new TestSessionContext { Id = "first-session" });
        using var secondClient = new GatedReplayChatClient(
            script,
            locks,
            new TestSessionContext { Id = "second-session" });

        await using var firstCall = firstClient.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")]).GetAsyncEnumerator();
        await using var nextCall = firstClient.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")]).GetAsyncEnumerator();
        await using var otherSessionCall = secondClient.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")]).GetAsyncEnumerator();

        Assert.IsTrue(await firstCall.MoveNextAsync());
        Assert.IsTrue(await nextCall.MoveNextAsync());
        Assert.IsTrue(await otherSessionCall.MoveNextAsync());
        var firstCompletion = firstCall.MoveNextAsync().AsTask();
        var nextCompletion = nextCall.MoveNextAsync().AsTask();
        var otherSessionCompletion = otherSessionCall.MoveNextAsync().AsTask();

        locks.Release($"first-session:{script.GetLockName(0, 0)}");

        Assert.IsFalse(await firstCompletion);
        Assert.IsFalse(nextCompletion.IsCompleted);
        Assert.IsFalse(otherSessionCompletion.IsCompleted);

        locks.Release($"first-session:{script.GetLockName(1, 0)}");
        locks.Release($"second-session:{script.GetLockName(0, 0)}");

        Assert.IsFalse(await nextCompletion);
        Assert.IsFalse(await otherSessionCompletion);
    }

    [TestMethod]
    public async Task AgenticChatScript_DecodesActionAndContinues()
    {
        var script = ReplayCheckpointScript.Load("Dojo_AgenticChat.recording.json");
        var locks = new TestLockProvider();
        const string sessionId = "agentic-chat-unit";
        for (var callIndex = 0; callIndex < script.Calls.Count; callIndex++)
        {
            for (var checkpointIndex = 0;
                checkpointIndex < script.Calls[callIndex].Frames.Count;
                checkpointIndex++)
            {
                locks.Release($"{sessionId}:{script.GetLockName(callIndex, checkpointIndex)}");
            }
        }

        string? capturedBackground = null;
        string ChangeBackground(string background)
        {
            capturedBackground = background;
            return "Background changed successfully.";
        }

        using var client = new GatedReplayChatClient(
            script,
            locks,
            new TestSessionContext { Id = sessionId });
        using var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(AIFunctionFactory.Create(
                (Func<string, string>)ChangeBackground,
                name: "change_background",
                description: "Change the background."));
        });
        var context = new AgentContext(agent);
        context.RegisterOnStatusChanged(status =>
        {
            if (status == ConversationStatus.AwaitingInput)
            {
                context.Turns[^1].ResponseBlocks
                    .OfType<UIActionBlock>()
                    .Single()
                    .InvokeAsync()
                    .GetAwaiter()
                    .GetResult();
            }
        });

        await context.SendMessageAsync("Change the background to something new");

        Assert.AreEqual("linear-gradient(135deg, #ff9a9e, #fad0c4)", capturedBackground);
        var finalText = context.Turns[^1].ResponseBlocks.OfType<RichContentBlock>().Single().RawText;
        Assert.AreEqual("Background changed to a sunset gradient.", finalText);
    }

    [TestMethod]
    public async Task BackendToolRenderingScript_DecodesToolCallAndContinues()
    {
        var script = ReplayCheckpointScript.Load("Dojo_BackendToolRendering.recording.json");
        var locks = new TestLockProvider();
        const string sessionId = "backend-tool-rendering-unit";
        for (var callIndex = 0; callIndex < script.Calls.Count; callIndex++)
        {
            for (var checkpointIndex = 0;
                checkpointIndex < script.Calls[callIndex].Frames.Count;
                checkpointIndex++)
            {
                locks.Release($"{sessionId}:{script.GetLockName(callIndex, checkpointIndex)}");
            }
        }

        string? capturedLocation = null;
        object GetWeather(string location)
        {
            capturedLocation = location;
            return new
            {
                Temperature = 20,
                Conditions = "sunny",
                Humidity = 50,
                WindSpeed = 10,
                FeelsLike = 25,
            };
        }

        using var client = new GatedReplayChatClient(
            script,
            locks,
            new TestSessionContext { Id = sessionId });
        using var agent = new UIAgent(
            client,
            new ChatOptions
            {
                Tools =
                [
                    AIFunctionFactory.Create(
                        (Func<string, object>)GetWeather,
                        name: "get_weather",
                        description: "Get the weather for a given location.")
                ],
            });
        var context = new AgentContext(agent);

        await context.SendMessageAsync("What is the weather in San Francisco?");

        Assert.AreEqual("San Francisco", capturedLocation);
        var turn = context.Turns.Single();
        var weatherBlock = turn.ResponseBlocks.OfType<FunctionInvocationContentBlock>().Single();
        Assert.AreEqual("get_weather", weatherBlock.ToolName);
        Assert.IsTrue(weatherBlock.HasResult);
        Assert.AreEqual("San Francisco", weatherBlock.Arguments!["location"]?.ToString());
        var finalText = turn.ResponseBlocks.OfType<RichContentBlock>().Single().RawText;
        Assert.AreEqual(
            "The weather in San Francisco is sunny with a temperature of 20\u00b0C.",
            finalText);
    }

    [TestMethod]
    public async Task HumanInTheLoopScript_DecodesSelectionResultAndContinues()
    {
        var script = ReplayCheckpointScript.Load("Dojo_HumanInTheLoop.recording.json");
        var locks = new TestLockProvider();
        const string sessionId = "human-in-the-loop-unit";
        for (var callIndex = 0; callIndex < script.Calls.Count; callIndex++)
        {
            for (var checkpointIndex = 0;
                checkpointIndex < script.Calls[callIndex].Frames.Count;
                checkpointIndex++)
            {
                locks.Release($"{sessionId}:{script.GetLockName(callIndex, checkpointIndex)}");
            }
        }

        IReadOnlyList<(string Description, string Status)>? capturedSteps = null;
        string GenerateTaskSteps(List<Dictionary<string, string>> steps)
        {
            capturedSteps = steps
                .Select(step => (step["description"], step["status"]))
                .ToList();
            var selected = capturedSteps
                .Where(step => step.Status != "disabled")
                .Select(step => step.Description);
            return $"The user selected the following steps: {string.Join(", ", selected)}";
        }

        using var client = new GatedReplayChatClient(
            script,
            locks,
            new TestSessionContext { Id = sessionId });
        using var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(AIFunctionFactory.Create(
                (Func<List<Dictionary<string, string>>, string>)GenerateTaskSteps,
                name: "generate_task_steps",
                description: "Generate task steps."));
        });
        var context = new AgentContext(agent);
        context.RegisterOnStatusChanged(status =>
        {
            if (status == ConversationStatus.AwaitingInput)
            {
                var block = context.Turns[^1].ResponseBlocks
                    .OfType<UIActionBlock>()
                    .Single();
                var steps = (JsonElement)block.Call!.Arguments!["steps"]!;
                var updatedSteps = steps.Deserialize<List<Dictionary<string, string>>>()!;
                updatedSteps[1]["status"] = "disabled";
                updatedSteps[2]["status"] = "enabled";
                block.Call.Arguments["steps"] = JsonSerializer.SerializeToElement(updatedSteps);
                block.InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync(
            "Help me organize a birthday party for my friend next Saturday. " +
            "Generate the task steps I need to complete.");

        Assert.IsNotNull(capturedSteps);
        Assert.HasCount(3, capturedSteps);
        Assert.AreEqual(("Book a party venue", "enabled"), capturedSteps[0]);
        Assert.AreEqual(("Order a birthday cake", "disabled"), capturedSteps[1]);
        Assert.AreEqual(("Send invitations", "enabled"), capturedSteps[2]);
        var turn = context.Turns.Single();
        var action = turn.ResponseBlocks.OfType<UIActionBlock>().Single();
        Assert.IsTrue(action.IsComplete);
        Assert.AreEqual("generate_task_steps", action.ToolName);
        var finalText = turn.ResponseBlocks.OfType<RichContentBlock>().Single().RawText;
        Assert.AreEqual(
            "I'll move forward with booking a party venue and sending invitations.",
            finalText);
    }

    [TestMethod]
    public async Task ToolBasedGenerativeUIScript_DecodesHaikuResultAndContinues()
    {
        var script = ReplayCheckpointScript.Load("Dojo_ToolBasedGenerativeUI.recording.json");
        var locks = new TestLockProvider();
        const string sessionId = "tool-based-generative-ui-unit";
        for (var callIndex = 0; callIndex < script.Calls.Count; callIndex++)
        {
            for (var checkpointIndex = 0;
                checkpointIndex < script.Calls[callIndex].Frames.Count;
                checkpointIndex++)
            {
                locks.Release($"{sessionId}:{script.GetLockName(callIndex, checkpointIndex)}");
            }
        }

        List<string>? capturedJapanese = null;
        List<string>? capturedEnglish = null;
        string? capturedImageName = null;
        string? capturedGradient = null;
        string GenerateHaiku(
            List<string> japanese,
            List<string> english,
            string image_name,
            string gradient)
        {
            capturedJapanese = japanese;
            capturedEnglish = english;
            capturedImageName = image_name;
            capturedGradient = gradient;
            return "Haiku displayed to user.";
        }

        using var client = new GatedReplayChatClient(
            script,
            locks,
            new TestSessionContext { Id = sessionId });
        using var agent = new UIAgent(client, options =>
        {
            options.RegisterUIAction(AIFunctionFactory.Create(
                (Func<List<string>, List<string>, string, string, string>)GenerateHaiku,
                name: "generate_haiku",
                description: "Generate a haiku."));
        });
        var context = new AgentContext(agent);
        context.RegisterOnStatusChanged(status =>
        {
            if (status == ConversationStatus.AwaitingInput)
            {
                context.Turns[^1].ResponseBlocks
                    .OfType<UIActionBlock>()
                    .Single()
                    .InvokeAsync()
                    .GetAwaiter()
                    .GetResult();
            }
        });

        await context.SendMessageAsync("Write me a haiku about nature");

        CollectionAssert.AreEqual(
            new[] { "古池や", "蛙飛びこむ", "水の音" },
            capturedJapanese);
        CollectionAssert.AreEqual(
            new[] { "An ancient pond\u2014", "A frog leaps in,", "The sound of water." },
            capturedEnglish);
        Assert.AreEqual("ancient-pond", capturedImageName);
        Assert.AreEqual("linear-gradient(135deg, #134e5e, #71b280)", capturedGradient);
        var turn = context.Turns.Single();
        var action = turn.ResponseBlocks.OfType<UIActionBlock>().Single();
        Assert.IsTrue(action.IsComplete);
        Assert.AreEqual("generate_haiku", action.ToolName);
        var finalText = turn.ResponseBlocks.OfType<RichContentBlock>().Single().RawText;
        Assert.AreEqual(
            "Your nature haiku is ready\u2014a quiet pond awakened by a frog.",
            finalText);
    }

    [TestMethod]
    public async Task AgenticGenerativeUIScript_MapsEveryPlanState()
    {
        var script = ReplayCheckpointScript.Load("Dojo_AgenticGenerativeUI.recording.json");
        var locks = new TestLockProvider();
        const string sessionId = "agentic-generative-ui-unit";
        for (var checkpointIndex = 0;
            checkpointIndex < script.Calls[0].Frames.Count;
            checkpointIndex++)
        {
            locks.Release($"{sessionId}:{script.GetLockName(0, checkpointIndex)}");
        }

        using var client = new GatedReplayChatClient(
            script,
            locks,
            new TestSessionContext { Id = sessionId });
        using var agent = new UIAgent<PlanState>(client, options =>
        {
            options.StateMapper = context =>
            {
                if (context.Update.RawRepresentation is not JsonElement snapshot)
                {
                    return false;
                }

                var state = snapshot.Deserialize<PlanState>(
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (state is null)
                {
                    return false;
                }

                context.SetState(state);
                return true;
            };
        });
        var states = new List<PlanState>();
        using var registration = agent.State.OnChanged(() => states.Add(agent.State.Value));
        var context = new AgentContext(agent);

        await context.SendMessageAsync("Create a plan for learning to bake bread");

        Assert.HasCount(6, states);
        for (var stateIndex = 0; stateIndex < states.Count; stateIndex++)
        {
            Assert.HasCount(5, states[stateIndex].Steps);
            Assert.AreEqual(
                stateIndex,
                states[stateIndex].Steps.Count(step => step.Status == "completed"));
        }

        CollectionAssert.AreEqual(
            new[] { "Gather ingredients", "Mix dough", "Let it rise", "Shape loaves", "Bake" },
            states[^1].Steps.Select(step => step.Description).ToArray());
        Assert.IsTrue(states[^1].Steps.All(step => step.Status == "completed"));
        Assert.AreEqual(0, context.Turns.Single().ResponseBlocks.Count);
    }

    private static async IAsyncEnumerable<ChatResponseUpdate> YieldAsync(
        IEnumerable<ChatResponseUpdate> updates,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }

        await Task.CompletedTask;
    }

    private static ReplayCheckpointScript CreateSingleFrameScript()
        => new()
        {
            Calls = [CreateSingleFrameCall()],
        };

    private static ReplayCall CreateSingleFrameCall()
        => new()
        {
            Frames =
            [
                new ReplayFrame
                {
                    Name = "only-frame",
                    Updates = [new ChatResponseUpdate(ChatRole.Assistant, "hello back")],
                },
            ],
        };
}
