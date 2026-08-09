// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace AIApp.Shared;

internal static class ManualChatClientConfiguration
{
    public const string CaptureEnabledEnvironmentVariable = "COMPONENTS_AI_CAPTURE_LIVE";
    public const string EndpointEnvironmentVariable = "COMPONENTS_AI_AZURE_OPENAI_ENDPOINT";
    public const string DeploymentEnvironmentVariable = "COMPONENTS_AI_AZURE_OPENAI_DEPLOYMENT";
    public const string CapturePathEnvironmentVariable = "COMPONENTS_AI_CAPTURE_PATH";
    public const string ReplayPathEnvironmentVariable = "COMPONENTS_AI_MANUAL_REPLAY_PATH";
    public const string DojoSimulationEnvironmentVariable = "COMPONENTS_AI_DOJO_SIMULATION";

    public static bool IsLiveCaptureEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(CaptureEnabledEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public static bool IsManualReplayEnabled =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ReplayPathEnvironmentVariable));

    public static bool IsDojoSimulationEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(DojoSimulationEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);

    public static void ValidateModeSelection()
    {
        var enabledModeCount =
            (IsLiveCaptureEnabled ? 1 : 0) +
            (IsManualReplayEnabled ? 1 : 0) +
            (IsDojoSimulationEnabled ? 1 : 0);
        if (enabledModeCount > 1)
        {
            throw new InvalidOperationException(
                "Live capture, manual replay, and dojo simulation modes are mutually exclusive.");
        }
    }

    public static CapturingChatClient CreateLiveCapture(
        string contentRootPath,
        Func<Uri, string, IChatClient> createClient,
        Action<Exception>? reportError = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);
        ArgumentNullException.ThrowIfNull(createClient);

        if (!IsLiveCaptureEnabled)
        {
            throw new InvalidOperationException(
                $"Live capture is disabled. Set {CaptureEnabledEnvironmentVariable}=true explicitly.");
        }

        if (IsManualReplayEnabled || IsDojoSimulationEnabled)
        {
            throw new InvalidOperationException(
                "Live capture cannot be combined with manual replay or dojo simulation.");
        }

        var endpoint = Environment.GetEnvironmentVariable(EndpointEnvironmentVariable);
        var deployment = Environment.GetEnvironmentVariable(DeploymentEnvironmentVariable);
        var capturePath = Environment.GetEnvironmentVariable(CapturePathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(endpoint) ||
            string.IsNullOrWhiteSpace(deployment) ||
            string.IsNullOrWhiteSpace(capturePath))
        {
            throw new InvalidOperationException(
                $"Live capture requires {EndpointEnvironmentVariable}, {DeploymentEnvironmentVariable}, " +
                $"and {CapturePathEnvironmentVariable}.");
        }

        var fullCapturePath = DecodedChatRecording.RequireAbsolutePath(capturePath, "Live capture");
        EnsureOutsideContentRoot(contentRootPath, fullCapturePath);

        return new CapturingChatClient(
            createClient(new Uri(endpoint), deployment),
            fullCapturePath,
            reportError,
            endpoint,
            deployment);
    }

    public static ManualReplayChatClient CreateManualReplay()
    {
        if (IsLiveCaptureEnabled || IsDojoSimulationEnabled)
        {
            throw new InvalidOperationException(
                "Manual replay cannot be combined with live capture or dojo simulation.");
        }

        var replayPath = Environment.GetEnvironmentVariable(ReplayPathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(replayPath))
        {
            throw new InvalidOperationException(
                $"Manual replay requires {ReplayPathEnvironmentVariable}.");
        }

        return new ManualReplayChatClient(DecodedChatRecording.Load(replayPath));
    }

    private static void EnsureOutsideContentRoot(string contentRootPath, string artifactPath)
    {
        var relativePath = Path.GetRelativePath(Path.GetFullPath(contentRootPath), artifactPath);
        if (!Path.IsPathRooted(relativePath) &&
            relativePath != ".." &&
            !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The capture artifact path must be outside the test app source directory.");
        }
    }
}

internal sealed class ManualReplayChatClient : IChatClient
{
    private readonly DecodedChatRecording _recording;
    private int _callIndex;

    public ManualReplayChatClient(DecodedChatRecording recording)
    {
        ArgumentNullException.ThrowIfNull(recording);
        _recording = recording;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestMessages = messages.ToList();
        if (_callIndex >= _recording.Calls.Count)
        {
            throw new InvalidOperationException("The manual replay recording has no remaining calls.");
        }

        var call = _recording.Calls[_callIndex];
        var expectedUserMessage = call.Messages.LastOrDefault(message => message.Role == ChatRole.User)?.Text;
        var actualUserMessage = requestMessages.LastOrDefault(message => message.Role == ChatRole.User)?.Text;
        if (!string.Equals(expectedUserMessage, actualUserMessage, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The manual replay request does not match the captured user message.");
        }

        _callIndex++;
        foreach (var update in call.Updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
            await Task.Yield();
        }
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetStreamingResponseAsync(messages, options, cancellationToken)
            .ToChatResponseAsync(cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(IChatClient) ? this : null;

    public void Dispose()
    {
    }
}
