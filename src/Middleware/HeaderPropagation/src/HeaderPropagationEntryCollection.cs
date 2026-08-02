// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation;

/// <summary>
/// A collection of <see cref="HeaderPropagationEntry"/> items.
/// </summary>
public sealed class HeaderPropagationEntryCollection : Collection<HeaderPropagationEntry>
{
    /// <summary>
    /// Adds an <see cref="HeaderPropagationEntry"/> that will use <paramref name="headerName"/> as
    /// the value of <see cref="HeaderPropagationEntry.InboundHeaderName"/> and
    /// <see cref="HeaderPropagationEntry.CapturedHeaderName"/>.
    /// </summary>
    /// <param name="headerName">The header name to be propagated.</param>
    public void Add(string headerName)
    {
        ArgumentNullException.ThrowIfNull(headerName);

        Add(new HeaderPropagationEntry(headerName, headerName, valueFilter: null));
    }

    /// <summary>
    /// Adds an <see cref="HeaderPropagationEntry"/> that will use <paramref name="headerName"/> as
    /// the value of <see cref="HeaderPropagationEntry.InboundHeaderName"/> and
    /// <see cref="HeaderPropagationEntry.CapturedHeaderName"/>.
    /// </summary>
    /// <param name="headerName">The header name to be propagated.</param>
    /// <param name="valueFilter">
    /// A filter delegate that can be used to transform the header value.
    /// See <see cref="HeaderPropagationEntry.ValueFilter"/>.
    /// </param>
    public void Add(string headerName, Func<HeaderPropagationContext, StringValues> valueFilter)
    {
        ArgumentNullException.ThrowIfNull(headerName);

        Add(new HeaderPropagationEntry(headerName, headerName, valueFilter));
    }

    /// <summary>
    /// Adds an <see cref="HeaderPropagationEntry"/> that will use the provided <paramref name="inboundHeaderName"/>
    /// and <paramref name="outboundHeaderName"/>.
    /// </summary>
    /// <param name="inboundHeaderName">
    /// The name of the header to be captured by <see cref="HeaderPropagationMiddleware"/>.
    /// </param>
    /// <param name="outboundHeaderName">
    /// The name of the header to be added by <see cref="HeaderPropagationMessageHandler"/>.
    /// </param>
    public void Add(string inboundHeaderName, string outboundHeaderName)
    {
        ArgumentNullException.ThrowIfNull(inboundHeaderName);
        ArgumentNullException.ThrowIfNull(outboundHeaderName);

        Add(new HeaderPropagationEntry(inboundHeaderName, outboundHeaderName, valueFilter: null));
    }

    /// <summary>
    /// Adds an <see cref="HeaderPropagationEntry"/> that will use the provided <paramref name="inboundHeaderName"/>,
    /// <paramref name="outboundHeaderName"/>, and <paramref name="valueFilter"/>.
    /// </summary>
    /// <param name="inboundHeaderName">
    /// The name of the header to be captured by <see cref="HeaderPropagationMiddleware"/>.
    /// </param>
    /// <param name="outboundHeaderName">
    /// The name of the header to be added by <see cref="HeaderPropagationMessageHandler"/>.
    /// </param>
    /// <param name="valueFilter">
    /// A filter delegate that can be used to transform the header value.
    /// See <see cref="HeaderPropagationEntry.ValueFilter"/>.
    /// </param>
    public void Add(
        string inboundHeaderName,
        string outboundHeaderName,
        Func<HeaderPropagationContext, StringValues> valueFilter)
    {
        ArgumentNullException.ThrowIfNull(inboundHeaderName);
        ArgumentNullException.ThrowIfNull(outboundHeaderName);

        Add(new HeaderPropagationEntry(inboundHeaderName, outboundHeaderName, valueFilter));
    }
}
