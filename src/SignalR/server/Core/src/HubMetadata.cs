// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Metadata that describes the <see cref="Hub"/> information associated with a specific endpoint.
/// </summary>
public class HubMetadata
{
    /// <summary>
    /// Constructs the <see cref="HubMetadata"/> of the given <see cref="Hub"/> type.
    /// </summary>
    /// <param name="hubType">The <see cref="Type"/> of the <see cref="Hub"/>.</param>
    public HubMetadata(Type hubType)
    {
        HubType = hubType;
    }

    /// <summary>
    /// The type of <see cref="Hub"/>.
    /// </summary>
    public Type HubType { get; }
}
