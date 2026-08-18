// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ClientModel;
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

        if (string.IsNullOrEmpty(baseUrl) && string.IsNullOrEmpty(apiKey))
        {
            return new ScriptedChatClient();
        }

        // Any OpenAI-compatible endpoint: the public OpenAI API, a local mock, or Azure OpenAI
        // through its OpenAI-compatible surface (https://{resource}.openai.azure.com/openai/v1/).
        var modelName = configuration["OPENAI_CHAT_MODEL_ID"] ?? "gpt-4o";

        var options = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(baseUrl))
        {
            options.Endpoint = new Uri(baseUrl);
        }

        var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey ?? string.Empty), options);

        return openAIClient.GetChatClient(modelName)
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }
}
