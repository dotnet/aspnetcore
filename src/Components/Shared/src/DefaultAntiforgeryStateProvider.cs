// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

internal class DefaultAntiforgeryStateProvider : AntiforgeryStateProvider
{
    [SupplyParameterFromPersistentComponentState]
    public AntiforgeryRequestToken? CurrentToken { get; set; }

    /// <inheritdoc />
    public override AntiforgeryRequestToken? GetAntiforgeryToken() => CurrentToken;
}
