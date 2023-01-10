// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation;

/// <summary>
/// A Middleware for propagating headers to an <see cref="HttpClient"/>.
/// </summary>
public class HeaderPropagationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HeaderPropagationOptions _options;
    private readonly HeaderPropagationValues _values;

    /// <summary>
    /// Initializes a new instance of <see cref="HeaderPropagationMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The <see cref="IOptions{HeaderPropagationOptions}"/>.</param>
    /// <param name="values">
    /// The <see cref="HeaderPropagationValues"/> that stores the request headers to be propagated in an <see cref="System.Threading.AsyncLocal{T}"/>
    /// </param>
    public HeaderPropagationMiddleware(RequestDelegate next, IOptions<HeaderPropagationOptions> options, HeaderPropagationValues values)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(values);

        _next = next;
        _options = options.Value;
        _values = values;
    }

    /// <summary>
    /// Executes the middleware that stores the request headers to be propagated in using <see cref="HeaderPropagationValues"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    public Task Invoke(HttpContext context)
    {
        // We need to intialize the headers because the message handler will use this to detect misconfiguration.
        var headers = _values.Headers ??= new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

        // Perf: avoid foreach since we don't define a struct enumerator.
        var entries = _options.Headers;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            // We intentionally process entries in order, and allow earlier entries to
            // take precedence over later entries when they have the same output name.
            if (!headers.ContainsKey(entry.CapturedHeaderName))
            {
                var value = GetValue(context, entry);
                if (!StringValues.IsNullOrEmpty(value))
                {
                    headers.Add(entry.CapturedHeaderName, value);
                }
            }
        }

        return _next.Invoke(context);
    }

    private static StringValues GetValue(HttpContext context, HeaderPropagationEntry entry)
    {
        context.Request.Headers.TryGetValue(entry.InboundHeaderName, out var value);
        if (entry.ValueFilter != null)
        {
            value = entry.ValueFilter(new HeaderPropagationContext(context, entry.InboundHeaderName, value));
        }

        return value;
    }
}
