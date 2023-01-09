// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.HeaderPropagation;

/// <summary>
/// A collection of <see cref="HeaderPropagationMessageHandlerEntry"/> items.
/// </summary>
public sealed class HeaderPropagationMessageHandlerEntryCollection : Collection<HeaderPropagationMessageHandlerEntry>
{
    /// <summary>
    /// Adds an <see cref="HeaderPropagationMessageHandlerEntry"/> that will use <paramref name="headerName"/> as
    /// the value of <see cref="HeaderPropagationMessageHandlerEntry.CapturedHeaderName"/> and
    /// <see cref="HeaderPropagationMessageHandlerEntry.OutboundHeaderName"/>.
    /// </summary>
    /// <param name="headerName">
    /// The name of the header to be added by the <see cref="HeaderPropagationMessageHandler"/>.
    /// </param>
    public void Add(string headerName)
    {
        ArgumentNullException.ThrowIfNull(headerName);

        Add(new HeaderPropagationMessageHandlerEntry(headerName, headerName));
    }

    /// <summary>
    /// Adds an <see cref="HeaderPropagationMessageHandlerEntry"/> that will use the provided <paramref name="capturedHeaderName"/>
    /// and <paramref name="outboundHeaderName"/>.
    /// </summary>
    /// <param name="capturedHeaderName">
    /// The name of the header captured by the <see cref="HeaderPropagationMiddleware"/>.
    /// </param>
    /// <param name="outboundHeaderName">
    /// The name of the header to be added by the <see cref="HeaderPropagationMessageHandler"/>.
    /// </param>
    public void Add(string capturedHeaderName, string outboundHeaderName)
    {
        ArgumentNullException.ThrowIfNull(capturedHeaderName);
        ArgumentNullException.ThrowIfNull(outboundHeaderName);

        Add(new HeaderPropagationMessageHandlerEntry(capturedHeaderName, outboundHeaderName));
    }
}
