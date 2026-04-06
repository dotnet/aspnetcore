// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;

namespace Microsoft.AspNetCore.Components.AI.Tests.TestHelpers;

internal sealed class NullAntiforgeryStateProvider : AntiforgeryStateProvider
{
    public override AntiforgeryRequestToken? GetAntiforgeryToken() => null;
}
