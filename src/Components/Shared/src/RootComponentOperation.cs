// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components;

internal sealed class RootComponentOperation
{
    // Represents the type of root component operation to perform.
    public RootComponentOperationType Type { get; set; }

    // The client side ID of the component to perform the operation on.
    public int SsrComponentId { get; set; }

    // The marker that was initially rendered to the page.
    public ComponentMarker? Marker { get; set; }

    // Describes additional information about the component.
    // This property may get populated by .NET after JSON deserialization.
    [JsonIgnore]
    public WebRootComponentDescriptor? Descriptor { get; set; }
}

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal sealed class WebRootComponentDescriptor(
    Type componentType,
    WebRootComponentParameters parameters)
{
    public Type ComponentType { get; } = componentType;

    public WebRootComponentParameters Parameters { get; } = parameters;

    private string GetDebuggerDisplay()
    {
        var parameters = string.Join(", ", Parameters.Parameters.ToDictionary().Select(p => $"{p.Key}: {p.Value}"));
        return $"{ComponentType.FullName}({parameters})";
    }
}
