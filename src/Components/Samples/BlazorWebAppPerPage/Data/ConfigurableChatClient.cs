// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.AI;

namespace BlazorWebAppPerPage.Data;

internal enum ChatProvider
{
    Echo,
    OpenAICompatible,
    AzureOpenAI,
}

internal sealed class ConfigurableChatClient : IChatClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly Uri _requestUri;
    private readonly string _model;
    private readonly string? _apiKey;
    private readonly ChatProvider _provider;
    private readonly DefaultAzureCredential? _azureCredential;

    public ConfigurableChatClient(Uri endpoint, string model, string? apiKey, ChatProvider provider)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        if (endpoint.Scheme is not ("https" or "http"))
        {
            throw new ArgumentException("The endpoint must use HTTP or HTTPS.", nameof(endpoint));
        }

        if (endpoint.Scheme == "http" && !endpoint.IsLoopback)
        {
            throw new ArgumentException("Remote endpoints must use HTTPS. HTTP is only allowed for localhost.", nameof(endpoint));
        }

        _provider = provider;
        _model = model;
        _apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;
        _azureCredential = provider == ChatProvider.AzureOpenAI && _apiKey is null
            ? new DefaultAzureCredential()
            : null;
        _requestUri = BuildRequestUri(endpoint, model, provider);
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(2),
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(messages, cancellationToken);
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var detail = responseBody.Length <= 2_000 ? responseBody : responseBody[..2_000];
            throw new HttpRequestException(
                $"The model endpoint returned {(int)response.StatusCode} ({response.ReasonPhrase}). {detail}",
                inner: null,
                response.StatusCode);
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(responseStream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var payload = line["data:".Length..].Trim();
            if (payload.Length == 0 || payload == "[DONE]")
            {
                continue;
            }

            var text = ReadTextDelta(payload);
            if (!string.IsNullOrEmpty(text))
            {
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    Contents = [new TextContent(text)],
                };
            }
        }
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This sample client uses streaming responses.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _requestUri);
        if (_apiKey is not null)
        {
            if (_provider == ChatProvider.AzureOpenAI)
            {
                request.Headers.Add("api-key", _apiKey);
            }
            else
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            }
        }
        else if (_azureCredential is not null)
        {
            var token = await _azureCredential.GetTokenAsync(
                new TokenRequestContext(["https://cognitiveservices.azure.com/.default"]),
                cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        }

        var requestMessages = messages.Select(message => new
        {
            role = message.Role.Value,
            content = message.Text,
        });

        object requestBody = _provider == ChatProvider.AzureOpenAI
            ? new
            {
                messages = requestMessages,
                stream = true,
            }
            : new
            {
                model = _model,
                messages = requestMessages,
                stream = true,
            };

        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, SerializerOptions),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static Uri BuildRequestUri(Uri endpoint, string model, ChatProvider provider)
    {
        if (provider == ChatProvider.AzureOpenAI)
        {
            if (endpoint.AbsolutePath.Contains("/openai/deployments/", StringComparison.OrdinalIgnoreCase))
            {
                return endpoint;
            }

            var builder = new UriBuilder(endpoint)
            {
                Path = $"{endpoint.AbsolutePath.TrimEnd('/')}/openai/deployments/{Uri.EscapeDataString(model)}/chat/completions",
                Query = "api-version=2024-10-21",
            };
            return builder.Uri;
        }

        if (endpoint.AbsolutePath.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        var path = endpoint.AbsolutePath.TrimEnd('/');
        path = path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) ? path : $"{path}/v1";
        return new UriBuilder(endpoint)
        {
            Path = $"{path}/chat/completions",
        }.Uri;
    }

    private static string? ReadTextDelta(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("choices", out var choices) ||
            choices.GetArrayLength() == 0)
        {
            return null;
        }

        var choice = choices[0];
        if (choice.TryGetProperty("delta", out var delta) &&
            delta.TryGetProperty("content", out var deltaContent) &&
            deltaContent.ValueKind == JsonValueKind.String)
        {
            return deltaContent.GetString();
        }

        if (choice.TryGetProperty("message", out var message) &&
            message.TryGetProperty("content", out var messageContent) &&
            messageContent.ValueKind == JsonValueKind.String)
        {
            return messageContent.GetString();
        }

        return null;
    }
}

internal sealed class SampleEchoChatClient : IChatClient
{
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = $"Echo: {messages.Last().Text}";
        foreach (var word in response.Split(' '))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents = [new TextContent($"{word} ")],
            };
            await Task.Delay(75, cancellationToken);
        }
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("This sample client uses streaming responses.");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
