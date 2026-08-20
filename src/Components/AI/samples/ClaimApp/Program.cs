// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using AGUI.Abstractions;
using AGUI.Client;
using AGUI.Formatting;
using AGUI.Server;
using ComponentsAIClaimApp.Components;
using ComponentsAIClaimApp.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

const string ClaimAgentEndpoint = "claim-agent";
const string ClaimAgentHttpClient = "claim-agent";

var builder = WebApplication.CreateBuilder(args);

var foundryOptions = new ClaimFoundryOptions();
builder.Configuration.GetSection("AzureOpenAI").Bind(foundryOptions);
foundryOptions.Endpoint ??= builder.Configuration["AZURE_OPENAI_ENDPOINT"];
foundryOptions.ApiKey ??= builder.Configuration["AZURE_OPENAI_API_KEY"];
foundryOptions.ChatDeployment =
    builder.Configuration["AZURE_OPENAI_CHAT_DEPLOYMENT"] ?? foundryOptions.ChatDeployment;
foundryOptions.TranscriptionDeployment =
    builder.Configuration["AZURE_OPENAI_TRANSCRIPTION_DEPLOYMENT"] ?? foundryOptions.TranscriptionDeployment;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.TryAddEnumerable(
    ServiceDescriptor.Singleton<IAGUIEventStreamFormatter, SseEventStreamFormatter>());
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Add(
        AIJsonUtilities.DefaultOptions.TypeInfoResolver!);
    options.SerializerOptions.TypeInfoResolverChain.Add(
        AGUIJsonSerializerContext.Default);
    AGUIJsonUtilities.RegisterInterruptContentTypes(options.SerializerOptions);
});
builder.Services.AddSingleton(foundryOptions);
builder.Services.AddSingleton<ClaimDamageAnalyzer>();
builder.Services.AddSingleton<IClaimAssistantBackend>(services =>
    services.GetRequiredService<ClaimDamageAnalyzer>());
builder.Services.AddSingleton<ClaimAgentChatClient>();
builder.Services.Configure<ClaimAgentOptions>(
    builder.Configuration.GetSection("ClaimAgent"));
builder.Services.AddHttpClient(ClaimAgentHttpClient, client =>
{
    client.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddScoped<IChatClient>(services =>
{
    var httpClient = services.GetRequiredService<IHttpClientFactory>()
        .CreateClient(ClaimAgentHttpClient);
    var options = services.GetRequiredService<IOptions<ClaimAgentOptions>>().Value;
    var navigationBaseUri = services.GetRequiredService<NavigationManager>().BaseUri;
    httpClient.BaseAddress = ClaimAgentAddress.Resolve(
        options.BaseAddress,
        navigationBaseUri);
    return new AGUIChatClient(
        new AGUIChatClientOptions(httpClient, ClaimAgentEndpoint));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAntiforgery();
app.MapStaticAssets();
app.MapPost(ClaimAgentEndpoint, (
    RunAgentInput input,
    IOptions<JsonOptions> jsonOptions,
    ClaimAgentChatClient chatClient,
    CancellationToken cancellationToken) =>
{
    var serializerOptions = jsonOptions.Value.SerializerOptions;
    var clientTools = input.Tools;
    ChatRequestContext context;
    try
    {
        // ClaimAgentChatClient emits client-tool calls. Hiding declarations here prevents
        // AGUI.Server mixed invocation from treating later user turns as continuations and
        // suppressing those function-call events.
        input.Tools = null;
        context = input.ToChatRequestContext(
            serializerOptions,
            new AGUIStreamOptions());
    }
    finally
    {
        input.Tools = clientTools;
    }

    context.ChatOptions.RawRepresentationFactory = _ => input;
    var updates = chatClient.GetStreamingResponseAsync(
        context.Messages,
        context.ChatOptions,
        cancellationToken);
    var events = updates.AsAGUIEventStreamAsync(context, cancellationToken);

    return new ClaimAgentEventStreamResult(
        events,
        new SseEventStreamFormatter(),
        cancellationToken);
})
    .WithMetadata(new ClaimAgentRequestSizeLimitMetadata());
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

file sealed class ClaimAgentRequestSizeLimitMetadata : IRequestSizeLimitMetadata
{
    public long? MaxRequestBodySize => ClaimLimits.MaximumSerializedRequestBytes;
}
