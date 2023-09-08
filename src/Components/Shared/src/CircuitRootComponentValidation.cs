// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

internal sealed class CircuitRootComponentValidation
{
    public Guid PrerenderId { get; set; }

    public int MaxComponentCount { get; set; }
}
