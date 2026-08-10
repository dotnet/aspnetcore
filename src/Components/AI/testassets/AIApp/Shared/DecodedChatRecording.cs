// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AIApp.Shared;

internal sealed class DecodedChatRecording
{
    internal static JsonSerializerOptions SerializerOptions { get; } = CreateSerializerOptions();

    public required List<CapturedChatCall> Calls { get; init; }

    public static DecodedChatRecording Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = RequireAbsolutePath(path, "Manual replay");
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Decoded chat recording not found: {fullPath}", fullPath);
        }

        return JsonSerializer.Deserialize<DecodedChatRecording>(
            File.ReadAllText(fullPath),
            SerializerOptions)
            ?? throw new InvalidOperationException($"Decoded chat recording decoded to null: {fullPath}");
    }

    internal static string RequireAbsolutePath(string path, string purpose)
    {
        if (!Path.IsPathFullyQualified(path))
        {
            throw new InvalidOperationException($"{purpose} requires an absolute artifact path.");
        }

        return Path.GetFullPath(path);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        return new JsonSerializerOptions(AIJsonUtilities.DefaultOptions)
        {
            WriteIndented = true,
        };
    }
}

internal sealed record CapturedChatCall(
    IReadOnlyList<ChatMessage> Messages,
    IReadOnlyList<ChatResponseUpdate> Updates);

internal sealed class CapturingChatClient : IChatClient
{
    private static readonly string[] _sensitiveMarkers =
    [
        "\"accessToken\"",
        "\"apiKey\"",
        "\"clientSecret\"",
        "Bearer ",
    ];

    private readonly IChatClient _inner;
    private readonly string? _capturePath;
    private readonly string[] _prohibitedValues;
    private readonly Action<Exception>? _reportError;
    private readonly List<CapturedChatCall> _calls = [];

    public CapturingChatClient(
        IChatClient inner,
        string? capturePath = null,
        Action<Exception>? reportError = null,
        params string[] prohibitedValues)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        _capturePath = capturePath;
        _reportError = reportError;
        _prohibitedValues = prohibitedValues;
    }

    public IReadOnlyList<CapturedChatCall> Calls => _calls;

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var capturedMessages = CloneWithReporting(messages.ToList());
        var capturedUpdates = new List<ChatResponseUpdate>();

        await using var enumerator = _inner.GetStreamingResponseAsync(
            capturedMessages, options, cancellationToken).GetAsyncEnumerator(cancellationToken);
        while (await MoveNextAsync(enumerator))
        {
            var update = enumerator.Current;
            capturedUpdates.Add(CloneUpdateForCapture(update));
            yield return update;
        }

        _calls.Add(new CapturedChatCall(capturedMessages, capturedUpdates));
        if (_capturePath is not null)
        {
            SaveRecording(_capturePath);
        }
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetStreamingResponseAsync(messages, options, cancellationToken)
            .ToChatResponseAsync(cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(IChatClient) ? this : _inner.GetService(serviceType, serviceKey);

    public void Dispose() => _inner.Dispose();

    public void SaveRecording(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = DecodedChatRecording.RequireAbsolutePath(path, "Live capture");
        var recording = new DecodedChatRecording { Calls = _calls };
        var json = JsonSerializer.Serialize(recording, DecodedChatRecording.SerializerOptions);
        EnsureSafeToWrite(json);

        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The capture artifact path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = $"{fullPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private async ValueTask<bool> MoveNextAsync(
        IAsyncEnumerator<ChatResponseUpdate> enumerator)
    {
        try
        {
            return await enumerator.MoveNextAsync();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _reportError?.Invoke(exception);
            throw;
        }
    }

    private void EnsureSafeToWrite(string json)
    {
        if (_prohibitedValues.Any(value =>
                !string.IsNullOrEmpty(value) &&
                json.Contains(value, StringComparison.OrdinalIgnoreCase)) ||
            _sensitiveMarkers.Any(marker =>
                json.Contains(marker, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "The decoded recording contains configuration or credential-like data and was not written.");
        }
    }

    private static T Clone<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, DecodedChatRecording.SerializerOptions);
        return JsonSerializer.Deserialize<T>(json, DecodedChatRecording.SerializerOptions)
            ?? throw new InvalidOperationException($"Could not decode a captured {typeof(T).Name}.");
    }

    private T CloneWithReporting<T>(T value)
    {
        try
        {
            return Clone(value);
        }
        catch (Exception exception)
        {
            _reportError?.Invoke(exception);
            throw;
        }
    }

    private ChatResponseUpdate CloneUpdateForCapture(ChatResponseUpdate update)
    {
        var clone = CloneWithReporting(update);
        clone.ModelId = null;
        clone.RawRepresentation = null;
        return clone;
    }
}
