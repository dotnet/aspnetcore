// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ClientModel;
using System.ComponentModel;
using System.Text.Json;
using AGUI.Abstractions;
using AGUI.Server;
using DojoAgent.AgenticGenerativeUI;
using DojoAgent.BackendToolRendering;
using DojoAgent.PredictiveStateUpdates;
using DojoAgent.SharedState;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

namespace DojoAgent;

// Resolves the model client each dojo endpoint runs against.
//
// A live model is used only when it is configured explicitly. Without configuration the API
// answers with a local scripted client, so running the dojo never reaches a paid service (or
// picks up ambient credentials) by accident. Browser tests replace this registration with a
// recorded client through a service override.
/// <summary>
/// Creates the model clients and scenario adapters shared by the dojo test assets.
/// </summary>
public static class ChatClientAgentFactory
{
    /// <summary>
    /// The media type of native predictive document state updates.
    /// </summary>
    public const string PredictiveStateMediaType =
        "application/vnd.aspnetcore.ai.predictive-state+json";

    /// <summary>
    /// The keyed model registration used by direct dojo scenarios.
    /// </summary>
    public const string ModelServiceKey = "dojo-model";

    /// <summary>
    /// The keyed raw model registration used by the predictive document scenario.
    /// </summary>
    public const string PredictiveStateUpdatesServiceKey = "predictive-state-updates-model";

    /// <summary>
    /// The instructions for generating a plan that the user reviews before execution.
    /// </summary>
    public const string HumanInTheLoopSystemPrompt = """
        You are a planning assistant.
        When asked to create a plan, call generate_task_steps so the user can review the steps.
        A request for a simple plan must contain exactly 5 sensible steps.
        A request for a complex plan must contain exactly 10 sensible steps.
        Keep all supported plans between 5 and 10 steps and set every initial status to "enabled".
        After the tool returns, mention exactly the selected steps and do not mention disabled steps as selected.
        If the user rejected every step, acknowledge that no steps will be performed.
        """;

    /// <summary>
    /// The instructions for generating a haiku through the UI tool.
    /// </summary>
    public const string ToolBasedGenerativeUISystemPrompt = """
        You are a Japanese haiku assistant.
        For every haiku request, call generate_haiku with exactly three Japanese lines, exactly
        three English translation lines, image_name set to ancient-pond.svg, and a two-color CSS
        linear-gradient written as linear-gradient(<angle>deg, <hex color>, <hex color>).
        Do not print the haiku as ordinary chat text before calling the tool.
        """;

    /// <summary>
    /// The instructions for creating and completing a shared plan through server tools.
    /// </summary>
    public const string AgenticGenerativeUISystemPrompt = """
        When planning use tools only, without any other messages.
        IMPORTANT:
        - Use the `create_plan` tool to set the initial state of the steps
        - Use the `update_plan_step` tool to update the status of each step
        - Do NOT repeat the plan or summarise it in a message
        - Do NOT confirm the creation or updates in a message
        - Do NOT ask the user for additional information or next steps
        - Do NOT leave a plan hanging, always complete the plan via `update_plan_step` if one is ongoing.
        - Continue calling update_plan_step until all steps are marked as completed.

        Only one plan can be active at a time, so do not call the `create_plan` tool
        again until all the steps in current plan are completed.
        """;

    /// <summary>
    /// The instructions for maintaining a recipe shared with the UI.
    /// </summary>
    public const string SharedStateSystemPrompt = """
        You are a helpful recipe assistant that maintains a shared recipe state with the user.

        IMPORTANT:
        - When the user asks you to create, change, or improve a recipe, call the
          `generate_recipe` tool with a COMPLETE recipe: a title, skill_level, cooking_time,
          special_preferences, the full list of ingredients (each with an icon, name and
          amount), and the step-by-step instructions.
        - Always include every ingredient the recipe needs.
        - When the user only asks a question about the recipe, answer in plain text and do
          NOT call the tool.
        """;

    /// <summary>
    /// The instructions for proposing document edits for user confirmation.
    /// </summary>
    public const string PredictiveStateUpdatesSystemPrompt = """
        You are a document editor assistant. When asked to write or edit content:

        IMPORTANT:
        - Use the `write_document_local` tool with the full document text in Markdown format
        - Format the document extensively so it's easy to read
        - You can use all kinds of markdown (headings, lists, bold, etc.)
        - However, do NOT use italic or strike-through formatting
        - You MUST write the full document, even when changing only a few words
        - When making edits to the document, try to make them minimal - do not change every word
        - Keep stories SHORT!

        After writing the document, briefly summarize the changes you made in at most two sentences.
        """;

    /// <summary>
    /// Creates the configured model with server-side function invocation enabled.
    /// </summary>
    /// <param name="configuration">The explicit model configuration, or empty configuration for offline use.</param>
    /// <returns>The model pipeline.</returns>
    public static IChatClient CreateAgenticChat(IConfiguration configuration)
        => CreateModelClient(configuration)
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

    /// <summary>
    /// Creates the configured raw model for predictive document updates.
    /// </summary>
    /// <param name="configuration">The explicit model configuration, or empty configuration for offline use.</param>
    /// <returns>The raw model client.</returns>
    public static IChatClient CreatePredictiveStateUpdates(IConfiguration configuration)
        => CreateModelClient(configuration);

    /// <summary>
    /// Adapts a model to a dojo scenario without using the AG-UI transport.
    /// </summary>
    /// <param name="model">The model pipeline owned by the service provider.</param>
    /// <param name="endpoint">The scenario's endpoint name.</param>
    /// <returns>A scoped scenario client that streams native AI content.</returns>
    public static IChatClient CreateDirect(IChatClient model, string endpoint)
        => new DirectDojoChatClient(model, endpoint);

    private static IChatClient CreateModelClient(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var baseUrl = configuration["OPENAI_BASE_URL"];
        var apiKey = configuration["OPENAI_API_KEY"];

        IChatClient modelClient;
        if (string.IsNullOrEmpty(baseUrl) && string.IsNullOrEmpty(apiKey))
        {
            modelClient = new ScriptedChatClient();
        }
        else
        {
            // Any OpenAI-compatible endpoint: the public OpenAI API, a local mock, or Azure OpenAI
            // through its OpenAI-compatible surface (https://{resource}.openai.azure.com/openai/v1/).
            var modelName = configuration["OPENAI_CHAT_MODEL_ID"] ?? "gpt-4o";

            var options = new OpenAIClientOptions();
            if (!string.IsNullOrEmpty(baseUrl))
            {
                options.Endpoint = new Uri(baseUrl);
            }

            var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey ?? string.Empty), options);
            modelClient = openAIClient.GetChatClient(modelName).AsIChatClient();
        }

        return modelClient;
    }

    /// <summary>
    /// Creates the server-owned weather tool.
    /// </summary>
    /// <param name="options">The tool argument and result serialization options.</param>
    /// <returns>The weather scenario's tools.</returns>
    public static IList<AITool> CreateBackendToolRenderingTools(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return
        [
            AIFunctionFactory.Create(
                GetWeather,
                name: "get_weather",
                description: "Get the weather for a given location.",
                options)
        ];
    }

    /// <summary>
    /// Creates the server-owned tools for creating and updating a plan.
    /// </summary>
    /// <param name="options">The tool argument and result serialization options.</param>
    /// <returns>The planning scenario's tools.</returns>
    public static IList<AITool> CreateAgenticGenerativeUITools(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return
        [
            AIFunctionFactory.Create(
                AgenticPlanningTools.CreatePlan,
                name: "create_plan",
                description: "Create a plan with multiple steps.",
                options),
            AIFunctionFactory.Create(
                AgenticPlanningTools.UpdatePlanStepAsync,
                name: "update_plan_step",
                description: "Update a step in the plan with new description or status.",
                options),
        ];
    }

    /// <summary>
    /// Maps planning tool results to AG-UI state snapshots and deltas.
    /// </summary>
    /// <returns>The planning scenario's AG-UI stream options.</returns>
    public static AGUIStreamOptions CreateAgenticGenerativeUIStreamOptions()
    {
        var options = new AGUIStreamOptions();
        options.MapResultAsStateSnapshot("create_plan");
        options.MapResultAsStateDelta("update_plan_step");

        return options;
    }

    /// <summary>
    /// Creates the server-owned recipe generation tool.
    /// </summary>
    /// <param name="options">The tool argument and result serialization options.</param>
    /// <returns>The shared recipe scenario's tools.</returns>
    public static IList<AITool> CreateSharedStateTools(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return
        [
            AIFunctionFactory.Create(
                GenerateRecipe,
                name: "generate_recipe",
                description: "Generate or update the shared recipe and display it to the user.",
                options),
        ];
    }

    /// <summary>
    /// Maps recipe tool results to AG-UI state snapshots.
    /// </summary>
    /// <returns>The shared recipe scenario's AG-UI stream options.</returns>
    public static AGUIStreamOptions CreateSharedStateStreamOptions()
    {
        var options = new AGUIStreamOptions();
        options.MapResultAsStateSnapshot("generate_recipe");

        return options;
    }

    /// <summary>
    /// Creates the server-owned document writing tool.
    /// </summary>
    /// <param name="options">The tool argument and result serialization options.</param>
    /// <returns>The predictive document scenario's tools.</returns>
    public static IList<AITool> CreatePredictiveStateUpdatesTools(
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return
        [
            AIFunctionFactory.Create(
                WriteDocument,
                name: "write_document_local",
                description: "Write a document using Markdown formatting.",
                options),
        ];
    }

    /// <summary>
    /// Maps document content and tool calls to predictive AG-UI state and confirmation events.
    /// </summary>
    /// <param name="options">The state serialization options.</param>
    /// <returns>The predictive document scenario's AG-UI stream options.</returns>
    public static AGUIStreamOptions CreatePredictiveStateUpdatesStreamOptions(
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var documentUpdates = new PredictiveDocumentUpdates();
        var streamOptions = new AGUIStreamOptions();
        streamOptions.MapContent(content => content is DataContent data &&
            data.MediaType == PredictiveStateMediaType
            ? [new StateSnapshotEvent
            {
                Snapshot = JsonSerializer.Deserialize<JsonElement>(data.Data.Span),
            }]
            : null);
        streamOptions.MapCall("write_document_local", call =>
        {
            if (documentUpdates.Create(call) is not { } change)
            {
                return [];
            }

            var events = new List<BaseEvent>();
            foreach (var state in change.Snapshots)
            {
                events.Add(new StateSnapshotEvent
                {
                    Snapshot = JsonSerializer.SerializeToElement(state, options),
                });
            }

            events.Add(new ToolCallResultEvent
            {
                MessageId = Guid.NewGuid().ToString("N"),
                ToolCallId = call.CallId,
                Content = "Document written.",
                Role = "tool",
            });

            if (change.Confirmation is { } confirmation)
            {
                events.Add(new ToolCallStartEvent
                {
                    ToolCallId = confirmation.CallId,
                    ToolCallName = confirmation.Name,
                    ParentMessageId = Guid.NewGuid().ToString("N"),
                });
                events.Add(new ToolCallArgsEvent
                {
                    ToolCallId = confirmation.CallId,
                    Delta = JsonSerializer.Serialize(confirmation.Arguments, options),
                });
                events.Add(new ToolCallEndEvent { ToolCallId = confirmation.CallId });
            }

            return events;
        });

        return streamOptions;
    }

    [Description("Get the weather for a given location.")]
    private static WeatherInfo GetWeather(
        [Description("The location to get the weather for.")] string location) => new()
        {
            Temperature = 20,
            Conditions = "sunny",
            Humidity = 50,
            WindSpeed = 10,
            FeelsLike = 25,
        };

    [Description("Generate or update the shared recipe and display it to the user.")]
    private static RecipeResponse GenerateRecipe(
        [Description("The complete recipe to display.")] Recipe recipe) => new()
        {
            Recipe = recipe,
        };

    [Description("Write a document in Markdown format.")]
    private static string WriteDocument(
        [Description("The complete document content.")] string document)
        => "Document written.";
}
