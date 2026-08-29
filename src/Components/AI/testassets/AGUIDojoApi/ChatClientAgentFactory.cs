// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ClientModel;
using System.ComponentModel;
using System.Text.Json;
using AGUI.Server;
using AGUIDojoApi.AgenticGenerativeUI;
using AGUIDojoApi.BackendToolRendering;
using AGUIDojoApi.SharedState;
using Microsoft.Extensions.AI;
using OpenAI;

namespace AGUIDojoApi;

// Resolves the model client each dojo endpoint runs against.
//
// A live model is used only when it is configured explicitly. Without configuration the API
// answers with a local scripted client, so running the dojo never reaches a paid service (or
// picks up ambient credentials) by accident. Browser tests replace this registration with a
// recorded client through a service override.
internal static class ChatClientAgentFactory
{
    internal const string HumanInTheLoopSystemPrompt = """
        You are a planning assistant.
        When asked to create a plan, call generate_task_steps so the user can review the steps.
        A request for a simple plan must contain exactly 5 sensible steps.
        A request for a complex plan must contain exactly 10 sensible steps.
        Keep all supported plans between 5 and 10 steps and set every initial status to "enabled".
        After the tool returns, mention exactly the selected steps and do not mention disabled steps as selected.
        If the user rejected every step, acknowledge that no steps will be performed.
        """;

    internal const string ToolBasedGenerativeUISystemPrompt = """
        You are a Japanese haiku assistant.
        For every haiku request, call generate_haiku with exactly three Japanese lines, exactly
        three English translation lines, image_name set to ancient-pond.svg, and a two-color CSS
        linear-gradient written as linear-gradient(<angle>deg, <hex color>, <hex color>).
        Do not print the haiku as ordinary chat text before calling the tool.
        """;

    internal const string AgenticGenerativeUISystemPrompt = """
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

    internal const string SharedStateSystemPrompt = """
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

    internal static IChatClient CreateAgenticChat(IConfiguration configuration)
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

        return modelClient
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }

    internal static IList<AITool> CreateBackendToolRenderingTools(JsonSerializerOptions options)
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

    internal static IList<AITool> CreateAgenticGenerativeUITools(JsonSerializerOptions options)
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

    internal static AGUIStreamOptions CreateAgenticGenerativeUIStreamOptions()
    {
        var options = new AGUIStreamOptions();
        options.MapResultAsStateSnapshot("create_plan");
        options.MapResultAsStateDelta("update_plan_step");

        return options;
    }

    internal static IList<AITool> CreateSharedStateTools(JsonSerializerOptions options)
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

    internal static AGUIStreamOptions CreateSharedStateStreamOptions()
    {
        var options = new AGUIStreamOptions();
        options.MapResultAsStateSnapshot("generate_recipe");

        return options;
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
}
