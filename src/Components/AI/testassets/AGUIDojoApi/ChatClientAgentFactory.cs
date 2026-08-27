// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ClientModel;
using System.ComponentModel;
using System.Text.Json;
using AGUIDojoApi.BackendToolRendering;
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
}
