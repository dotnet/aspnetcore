// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation;

/// <summary>
/// Define the configuration of a header for the <see cref="HeaderPropagationMiddleware"/>.
/// </summary>
public class HeaderPropagationEntry
{
    /// <summary>
    /// Creates a new <see cref="HeaderPropagationEntry"/> with the provided <paramref name="inboundHeaderName"/>,
    /// <paramref name="capturedHeaderName"/> and <paramref name="valueFilter"/>.
    /// </summary>
    /// <param name="inboundHeaderName">
    /// The name of the header to be captured by <see cref="HeaderPropagationMiddleware"/>.
    /// </param>
    ///  <param name="capturedHeaderName">
    /// The name of the header to be added by <see cref="HeaderPropagationMessageHandler"/>.
    /// </param>
    /// <param name="valueFilter">
    /// A filter delegate that can be used to transform the header value. May be null.
    /// </param>
    public HeaderPropagationEntry(
        string inboundHeaderName,
        string capturedHeaderName,
        Func<HeaderPropagationContext, StringValues>? valueFilter)
    {
        ArgumentNullException.ThrowIfNull(inboundHeaderName);
        ArgumentNullException.ThrowIfNull(capturedHeaderName);

        InboundHeaderName = inboundHeaderName;
        CapturedHeaderName = capturedHeaderName;
        ValueFilter = valueFilter; // May be null
    }

    /// <summary>
    /// Gets the name of the header that will be captured by the <see cref="HeaderPropagationMiddleware"/>.
    /// </summary>
    public string InboundHeaderName { get; }

    /// <summary>
    /// Gets the name of the header to be used by default by the <see cref="HeaderPropagationMessageHandler"/> for the
    /// outbound http requests.
    /// </summary>
    public string CapturedHeaderName { get; }

    /// <summary>
    /// Gets or sets a filter delegate that can be used to transform the header value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When present, the delegate will be evaluated once per request to provide the transformed
    /// header value. The delegate will be called regardless of whether a header with the name
    /// corresponding to <see cref="InboundHeaderName"/> is present in the request. If the result
    /// of evaluating <see cref="ValueFilter"/> is null or empty, it will not be added to the propagated
    /// values.
    /// </para>
    /// </remarks>
    public Func<HeaderPropagationContext, StringValues>? ValueFilter { get; }
}
