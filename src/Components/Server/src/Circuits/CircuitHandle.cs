// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

// Used to isolate a circuit from a CircuitHost.
//
// We can't refer to Hub.Items from a CircuitHost - but we want need to be
// able to break the link between Hub.Items and a CircuitHost.
internal sealed class CircuitHandle
{
    public CircuitHost CircuitHost { get; set; }
}
