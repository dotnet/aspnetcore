// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

internal class DefaultAntiforgeryStateProvider : AntiforgeryStateProvider
{
    protected AntiforgeryRequestToken? _currentToken;

    [SupplyParameterFromPersistentComponentState]
    public AntiforgeryRequestToken? CurrentToken
    {
        get => _currentToken ??= GetAntiforgeryToken();
        set => _currentToken = value;
    }

    /// <inheritdoc />
    public override AntiforgeryRequestToken? GetAntiforgeryToken() => _currentToken;
}
