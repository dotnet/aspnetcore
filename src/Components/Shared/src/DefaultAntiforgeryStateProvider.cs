// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Forms;

internal class DefaultAntiforgeryStateProvider : AntiforgeryStateProvider, IDisposable
{
    private const string PersistenceKey = $"__internal__{nameof(AntiforgeryRequestToken)}";
    private readonly PersistingComponentStateSubscription _subscription;
    private readonly AntiforgeryRequestToken? _currentToken;

    [UnconditionalSuppressMessage(
    "Trimming",
    "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
    Justification = $"{nameof(DefaultAntiforgeryStateProvider)} uses the {nameof(PersistentComponentState)} APIs to deserialize the token, which are already annotated.")]
    public DefaultAntiforgeryStateProvider(PersistentComponentState state)
    {
        // Automatically flow the Request token to server/wasm through
        // persistent component state. This guarantees that the antiforgery
        // token is available on the interactive components, even when they
        // don't have access to the request.
        _subscription = state.RegisterOnPersisting(() =>
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(
                GetAntiforgeryToken(),
                DefaultAntiforgeryStateProviderSerializerContext.Default.AntiforgeryRequestToken);
            state.PersistAsJson(PersistenceKey, bytes);
            return Task.CompletedTask;
        }, RenderMode.InteractiveAuto);

        if (state.TryTakeFromJson<byte[]>(PersistenceKey, out var bytes))
        {
            _currentToken = JsonSerializer.Deserialize(
                bytes,
                DefaultAntiforgeryStateProviderSerializerContext.Default.AntiforgeryRequestToken);
        }
    }

    /// <inheritdoc />
    public override AntiforgeryRequestToken? GetAntiforgeryToken() => _currentToken;

    /// <inheritdoc />
    public void Dispose() => _subscription.Dispose();
}

[JsonSerializable(typeof(AntiforgeryRequestToken))]
internal sealed partial class DefaultAntiforgeryStateProviderSerializerContext : JsonSerializerContext;
