// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using AIApp.Components.Scenarios.AgenticGenerativeUI;
using AIApp.E2E.Tests.ServiceOverrides;
using AIApp.Shared;
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
            return YieldAsync([
                new ChatResponseUpdate(ChatRole.Assistant, "captured")
                {
                    ModelId = "deployment-name",
                    RawRepresentation = new { Credential = "provider metadata" },
                },
            ], cancellationToken);
        });
        using var client = new CapturingChatClient(fake);

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")]);

        Assert.AreEqual("captured", response.Text);
        Assert.HasCount(1, client.Calls);
        Assert.AreEqual("hello", client.Calls[0].Messages.Single().Text);
        Assert.AreEqual("captured", client.Calls[0].Updates.Single().Text);
        Assert.IsNull(client.Calls[0].Updates.Single().ModelId);
        Assert.IsNull(client.Calls[0].Updates.Single().RawRepresentation);
    }

    [TestMethod]
    public async Task CapturingChatClient_SavesDecodedRecordingForOfflineReplay()
    {
        var recordingPath = Path.Combine(
            Path.GetTempPath(),
            $"AIApp-{Guid.NewGuid():N}.recording.json");
        try
        {
            var fake = new FakeChatClient();
            fake.Enqueue((_, _, cancellationToken) =>
                YieldAsync(
                    [
                        new ChatResponseUpdate(ChatRole.Assistant, "captured "),
                        new ChatResponseUpdate(ChatRole.Assistant, "response"),
                    ],
                    cancellationToken));
            using (var capture = new CapturingChatClient(fake, recordingPath))
            {
                var response = await capture.GetResponseAsync(
                    [new ChatMessage(ChatRole.User, "hello")]);
                Assert.AreEqual("captured response", response.Text);
            }

            using var replay = new ManualReplayChatClient(
                DecodedChatRecording.Load(recordingPath));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                replay.GetResponseAsync([new ChatMessage(ChatRole.User, "different")]));

            var replayedResponse = await replay.GetResponseAsync(
                [new ChatMessage(ChatRole.User, "hello")]);

            Assert.AreEqual("captured response", replayedResponse.Text);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                replay.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]));
        }
        finally
        {
            File.Delete(recordingPath);
        }
    }

    [TestMethod]
    public async Task CapturingChatClient_RejectsCredentialLikeRecording()
    {
        var recordingPath = Path.Combine(
            Path.GetTempPath(),
            $"AIApp-{Guid.NewGuid():N}.recording.json");
        try
        {
            var fake = new FakeChatClient();
            fake.Enqueue((_, _, cancellationToken) =>
                YieldAsync(
                    [new ChatResponseUpdate(ChatRole.Assistant, "configured-endpoint")],
                    cancellationToken));
            using var client = new CapturingChatClient(
                fake,
                recordingPath,
                reportError: null,
                "configured-endpoint");

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]));
            Assert.IsFalse(File.Exists(recordingPath));
        }
        finally
        {
            File.Delete(recordingPath);
        }
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
    public async Task GatedReplayChatClient_ReplaysWithoutSession()
    {
        var script = CreateSingleFrameScript();
        using var client = new GatedReplayChatClient(
            script,
            new TestLockProvider(),
            new TestSessionContext());

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")]);

        Assert.AreEqual("hello back", response.Text);
    }

    [TestMethod]
    public async Task GatedReplayChatClient_ResetStartsNewGenerationAtFirstCall()
    {
        var script = CreateSingleFrameScript();
        var locks = new TestLockProvider();
        var replayState = new ReplayCheckpointState();
        const string sessionId = "reset-session";
        using var client = new GatedReplayChatClient(
            script,
            locks,
            new TestSessionContext { Id = sessionId },
            checkpointState: replayState);

        await AssertSingleFrameCallAsync(client, locks, script, sessionId, generation: 0);

        replayState.ResetReplay();

        await AssertSingleFrameCallAsync(client, locks, script, sessionId, generation: 1);
    }

    [TestMethod]
    public void ReplayCheckpointState_RejectsResetWhileCallIsActive()
    {
        var replayState = new ReplayCheckpointState();
        var changes = 0;
        replayState.Changed += () => changes++;

        Assert.IsFalse(replayState.IsReplayActive);
        Assert.AreEqual(0, replayState.BeginReplayCall());
        Assert.IsTrue(replayState.IsReplayActive);
        Assert.AreEqual(1, changes);
        Assert.Throws<InvalidOperationException>(replayState.ResetReplay);

        replayState.EndReplayCall();
        Assert.IsFalse(replayState.IsReplayActive);
        Assert.AreEqual(2, changes);
        replayState.ResetReplay();

        Assert.AreEqual(1, replayState.Generation);
        Assert.AreEqual(3, changes);
        Assert.AreEqual(0, replayState.BeginReplayCall());
        Assert.IsTrue(replayState.IsReplayActive);
        replayState.EndReplayCall();
        Assert.IsFalse(replayState.IsReplayActive);
        Assert.AreEqual(5, changes);
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
                updatedSteps[3]["status"] = "disabled";
                block.Call.Arguments["steps"] = JsonSerializer.SerializeToElement(updatedSteps);
                block.InvokeAsync().GetAwaiter().GetResult();
            }
        });

        await context.SendMessageAsync("Please plan a trip to mars in 5 steps.");

        Assert.IsNotNull(capturedSteps);
        Assert.HasCount(5, capturedSteps);
        Assert.AreEqual(("Define mission goals and timeline", "enabled"), capturedSteps[0]);
        Assert.AreEqual(("Design and test the spacecraft", "disabled"), capturedSteps[1]);
        Assert.AreEqual(("Select and train the astronaut crew", "enabled"), capturedSteps[2]);
        Assert.AreEqual(("Plan launch and Mars surface operations", "disabled"), capturedSteps[3]);
        Assert.AreEqual(
            ("Prepare communications and contingency plans", "enabled"),
            capturedSteps[4]);
        var turn = context.Turns.Single();
        var action = turn.ResponseBlocks.OfType<UIActionBlock>().Single();
        Assert.IsTrue(action.IsComplete);
        Assert.AreEqual("generate_task_steps", action.ToolName);
        var finalText = turn.ResponseBlocks.OfType<RichContentBlock>().Single().RawText;
        Assert.AreEqual(
            "I'll move forward with the selected tasks: Define mission goals and timeline, " +
            "Select and train the astronaut crew, " +
            "Prepare communications and contingency plans.",
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
            options.ChatOptions = new ChatOptions
            {
                Tools =
                [
                    AIFunctionFactory.Create(
                        () => "Plan created.",
                        name: "create_plan"),
                    AIFunctionFactory.Create(
                        () => "Plan step updated.",
                        name: "update_plan_step"),
                ],
            };

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

        await context.SendMessageAsync("Please build a plan to go to mars in 5 steps.");

        Assert.HasCount(6, states);
        for (var stateIndex = 0; stateIndex < states.Count; stateIndex++)
        {
            Assert.HasCount(5, states[stateIndex].Steps);
            Assert.AreEqual(
                stateIndex,
                states[stateIndex].Steps.Count(step => step.Status == "completed"));
        }

        CollectionAssert.AreEqual(
            new[]
            {
                "Develop a comprehensive mission plan, detailing objectives, budget, and timeline.",
                "Design and test a spacecraft capable of transporting humans and cargo to Mars.",
                "Select and train astronaut crew for the mission.",
                "Establish communication systems and infrastructure for Mars exploration.",
                "Launch the spacecraft and execute the mission to Mars.",
            },
            states[^1].Steps.Select(step => step.Description).ToArray());
        Assert.IsTrue(states[^1].Steps.All(step => step.Status == "completed"));
        Assert.AreEqual(
            "All five steps in the Mars mission plan are complete.",
            context.Turns.Single().ResponseBlocks.OfType<RichContentBlock>().Single().RawText);
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

    private static async Task AssertSingleFrameCallAsync(
        GatedReplayChatClient client,
        TestLockProvider locks,
        ReplayCheckpointScript script,
        string sessionId,
        int generation)
    {
        await using var enumerator = client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")]).GetAsyncEnumerator();

        Assert.IsTrue(await enumerator.MoveNextAsync());
        Assert.AreEqual("hello back", enumerator.Current.Text);

        var completion = enumerator.MoveNextAsync().AsTask();
        Assert.IsFalse(completion.IsCompleted);
        var lockName = GatedReplayChatClient.GetGenerationLockName(
            generation,
            script.GetLockName(0, 0));
        locks.Release($"{sessionId}:{lockName}");

        Assert.IsFalse(await completion);
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
