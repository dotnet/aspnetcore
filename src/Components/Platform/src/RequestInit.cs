// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Platform;

/// <summary>
/// Configures a browser Fetch request.
/// </summary>
public sealed class RequestInit
{
    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public string? Method { get; init; }

    /// <summary>
    /// Gets or sets request headers.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>
    /// Gets or sets the request body.
    /// </summary>
    public string? Body { get; init; }

    internal Dictionary<string, object?> ToJavaScriptValue()
    {
        var value = new Dictionary<string, object?>();

        if (Method is not null)
        {
            value["method"] = Method;
        }

        if (Headers is not null)
        {
            value["headers"] = new Dictionary<string, string>(Headers);
        }

        if (Body is not null)
        {
            value["body"] = Body;
        }

        return value;
    }
}
