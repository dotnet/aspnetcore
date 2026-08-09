// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AIApp.Components;
using AIApp.Shared;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

ManualChatClientConfiguration.ValidateModeSelection();

if (ManualChatClientConfiguration.IsDojoLiveAgentEnabled)
{
    var (endpoint, deployment) =
        ManualChatClientConfiguration.GetAzureOpenAIConfiguration("Dojo live-agent mode");
    builder.Services.AddSingleton<IDojoLiveAgentDelay>(
        new DojoLiveAgentDelay(TimeSpan.FromMilliseconds(50)));
    builder.Services.AddScoped<IChatClient>(services =>
        new DojoLiveAgentChatClient(
            new AzureOpenAIClient(endpoint, CreateDefaultAzureCredential())
                .GetChatClient(deployment)
                .AsIChatClient(),
            services.GetRequiredService<IDojoLiveAgentDelay>(),
            services.GetRequiredService<ILogger<DojoLiveAgentChatClient>>()));
}
else if (ManualChatClientConfiguration.IsLiveCaptureEnabled)
{
    builder.Services.AddScoped<IChatClient>(services =>
        ManualChatClientConfiguration.CreateLiveCapture(
            builder.Environment.ContentRootPath,
            static (endpoint, deployment) =>
                new AzureOpenAIClient(endpoint, CreateDefaultAzureCredential())
                    .GetChatClient(deployment)
                    .AsIChatClient(),
            exception =>
            {
                var status = exception.GetType().GetProperty("Status")?.GetValue(exception)
                    ?? "unavailable";
                services.GetRequiredService<ILogger<CapturingChatClient>>().LogError(
                    "Live capture failed. Exception type: {ExceptionType}; status: {Status}; " +
                    "inner exception type: {InnerExceptionType}.",
                    exception.GetType().FullName,
                    status,
                    exception.InnerException?.GetType().FullName ?? "none");
            }));
}
else if (ManualChatClientConfiguration.IsManualReplayEnabled)
{
    builder.Services.AddScoped<IChatClient>(_ =>
        ManualChatClientConfiguration.CreateManualReplay());
}
else if (ManualChatClientConfiguration.IsDojoSimulationEnabled)
{
    builder.Services.AddSingleton<IDojoSimulationDelay>(
        new DojoSimulationDelay(TimeSpan.FromMilliseconds(750)));
    builder.Services.AddScoped<IChatClient, DojoSimulationChatClient>();
}
else
{
    builder.Services.AddSingleton<IChatClient>(new EchoChatClient());
}

builder.Services.AddSingleton<ScenarioRegistry>();
builder.Services.AddScoped<ReplayCheckpointState>();

var app = builder.Build();

app.Logger.LogInformation(
    "AIApp chat client mode: {ChatClientMode}.",
    ManualChatClientConfiguration.IsDojoLiveAgentEnabled
        ? "dojo live agent"
        : ManualChatClientConfiguration.IsLiveCaptureEnabled
            ? "live capture"
            : ManualChatClientConfiguration.IsManualReplayEnabled
                ? "manual replay"
                : ManualChatClientConfiguration.IsDojoSimulationEnabled
                    ? "dojo simulation"
                    : "echo");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static DefaultAzureCredential CreateDefaultAzureCredential()
{
    return new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        ExcludeEnvironmentCredential = true,
        ExcludeWorkloadIdentityCredential = true,
        ExcludeManagedIdentityCredential = true,
        ExcludeSharedTokenCacheCredential = true,
        ExcludeVisualStudioCredential = true,
        ExcludeVisualStudioCodeCredential = true,
        ExcludeAzurePowerShellCredential = true,
        ExcludeAzureDeveloperCliCredential = true,
        ExcludeInteractiveBrowserCredential = true,
    });
}

// Default echo client — returns the user's message back
internal sealed class EchoChatClient : IChatClient
{
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var lastMessage = messages.Last();
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            Contents = [new TextContent($"Echo: {lastMessage.Text}")]
        };
        await Task.CompletedTask;
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
