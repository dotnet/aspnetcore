// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HeaderPropagation;

/// <summary>
/// Define the configuration of an header for the <see cref="HeaderPropagationMessageHandler"/>.
/// </summary>
public class HeaderPropagationMessageHandlerEntry
{
    /// <summary>
    /// Creates a new <see cref="HeaderPropagationMessageHandlerEntry"/> with the provided <paramref name="capturedHeaderName"/>
    /// and <paramref name="outboundHeaderName"/>.
    /// </summary>
    /// <param name="capturedHeaderName">
    /// The name of the header to be used to lookup the headers captured by the <see cref="HeaderPropagationMiddleware"/>.
    /// </param>
    /// <param name="outboundHeaderName">
    /// The name of the header to be added to the outgoing http requests by the <see cref="HeaderPropagationMessageHandler"/>.
    /// </param>
    public HeaderPropagationMessageHandlerEntry(
        string capturedHeaderName,
        string outboundHeaderName)
    {
        ArgumentNullException.ThrowIfNull(capturedHeaderName);
        ArgumentNullException.ThrowIfNull(outboundHeaderName);

        CapturedHeaderName = capturedHeaderName;
        OutboundHeaderName = outboundHeaderName;
    }

    /// <summary>
    /// Gets the name of the header to be used to lookup the headers captured by the <see cref="HeaderPropagationMiddleware"/>.
    /// </summary>
    public string CapturedHeaderName { get; }

    /// <summary>
    /// Gets the name of the header to be added to the outgoing http requests by the <see cref="HeaderPropagationMessageHandler"/>.
    /// </summary>
    public string OutboundHeaderName { get; }
}
