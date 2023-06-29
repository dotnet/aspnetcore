// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Provides access to the antiforgery token associated with the current session.
/// </summary>
public class AntiforgeryStateProvider : IDisposable
{
    private const string PersistenceKey = nameof(AntiforgeryRequestToken);
    private readonly PersistingComponentStateSubscription _subscription;
    private readonly AntiforgeryRequestToken? _currentToken;

    /// <summary>
    /// Initializes a new instance of <see cref="AntiforgeryStateProvider"/>.
    /// </summary>
    /// <param name="state">The <see cref="PersistentComponentState"/> associated with the current session.</param>
    [UnconditionalSuppressMessage(
    "Trimming",
    "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
    Justification = $"{nameof(AntiforgeryStateProvider)} uses the {nameof(PersistentComponentState)} APIs to deserialize the token, which are already annotated.")]
    public AntiforgeryStateProvider(PersistentComponentState state)
    {
        // Automatically flow the Request token to server/wasm through
        // persistent component state. This guarantees that the antiforgery
        // token is available on the interactive components, even when they
        // don't have access to the request.
        _subscription = state.RegisterOnPersisting(() =>
        {
            state.PersistAsJson(PersistenceKey, _currentToken);
            return Task.CompletedTask;
        });

        state.TryTakeFromJson(PersistenceKey, out _currentToken);
    }

    /// <summary>
    /// Gets the current <see cref="AntiforgeryRequestToken"/> if available.
    /// </summary>
    /// <returns>The current <see cref="AntiforgeryRequestToken"/> if available.</returns>
    public virtual AntiforgeryRequestToken? GetAntiforgeryToken() => _currentToken;

    /// <inheritdoc />
    public void Dispose() => _subscription.Dispose();
}
