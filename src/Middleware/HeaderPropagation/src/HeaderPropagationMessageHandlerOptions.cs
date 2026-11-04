// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HeaderPropagation;

/// <summary>
/// Provides configuration for the <see cref="HeaderPropagationMessageHandler"/>.
/// </summary>
public class HeaderPropagationMessageHandlerOptions
{
    /// <summary>
    /// Gets or sets the headers to be propagated by the <see cref="HeaderPropagationMessageHandler"/>.
    /// </summary>
    /// <remarks>
    /// If <see cref="Headers"/> is empty, all the headers captured by the <see cref="HeaderPropagationMiddleware"/> are propagated.
    /// Entries in <see cref="Headers"/> are processed in order while adding headers inside
    /// <see cref="HeaderPropagationMessageHandler"/>. This can cause an earlier entry to take precedence
    /// over a later entry if they have the same <see cref="HeaderPropagationMessageHandlerEntry.OutboundHeaderName"/>.
    /// </remarks>
    public HeaderPropagationMessageHandlerEntryCollection Headers { get; set; } = new HeaderPropagationMessageHandlerEntryCollection();
}
