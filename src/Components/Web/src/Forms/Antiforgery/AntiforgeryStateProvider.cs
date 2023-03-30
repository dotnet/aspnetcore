// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

public class AntiforgeryStateProvider : IDisposable
{
    private const string PersistenceKey = nameof(AntiforgeryRequestToken);
    private readonly PersistingComponentStateSubscription _subscription;
    private AntiforgeryRequestToken _currentToken;

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

    public AntiforgeryRequestToken GetAntiforgeryToken() { return _currentToken; }

    public event Action<AntiforgeryRequestToken>? AntiforgeryTokenChanged;

    protected void NotifyCurrentTokenChanged(AntiforgeryRequestToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        _currentToken = token;
        AntiforgeryTokenChanged?.Invoke(token);
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
