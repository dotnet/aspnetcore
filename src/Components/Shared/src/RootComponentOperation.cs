// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

internal sealed class RootComponentOperation
{
    // Represents the type of root component operation to perform.
    public RootComponentOperationType Type { get; set; }

    // The client side ID of the component to perform the operation on.
    public int SsrComponentId { get; set; }

    // The marker that was initially rendered to the page.
    public ComponentMarker? Marker { get; set; }
}
