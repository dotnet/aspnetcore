// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

internal sealed class RootComponentOperation
{
    // Represents the type of root component operation to perform.
    public RootComponentOperationType Type { get; set; }

    // When adding a root component, this is the selector ID
    // to round-trip back to the client so it knows which DOM
    // element the component should be attached to.
    public int? SelectorId { get; set; }

    // The ID of the component to use during an update or remove
    // operation.
    public int? ComponentId { get; set; }

    // The marker that was initially rendered to the page.
    public ComponentMarker? Marker { get; set; }
}
