// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation;

/// <summary>
/// Contains the outbound header values for the <see cref="HeaderPropagationMessageHandler"/>.
/// </summary>
public class HeaderPropagationValues
{
    private static readonly AsyncLocal<IDictionary<string, StringValues>?> _headers = new AsyncLocal<IDictionary<string, StringValues>?>();

    /// <summary>
    /// Gets or sets the headers values collected by the <see cref="HeaderPropagationMiddleware"/> from the current request
    /// that can be propagated.
    /// </summary>
    /// <remarks>
    /// The keys of <see cref="Headers"/> correspond to <see cref="HeaderPropagationEntry.CapturedHeaderName"/>.
    /// </remarks>
    public IDictionary<string, StringValues>? Headers
    {
        get
        {
            return _headers.Value;
        }
        set
        {
            _headers.Value = value;
        }
    }
}
