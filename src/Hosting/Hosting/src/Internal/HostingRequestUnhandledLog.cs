// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class HostingRequestUnhandledLog : IReadOnlyList<KeyValuePair<string, object?>>
{
    private const string OriginalFormat = "Request reached the end of the middleware pipeline without being handled by application code. Request path: {Method} {Scheme}://{Host}{PathBase}{Path}, Response status code: {StatusCode}";

    internal static readonly Func<object, Exception?, string> Callback = (state, exception) => ((HostingRequestUnhandledLog)state).ToString();

    private readonly HttpContext _httpContext;

    private string? _cachedToString;

    public int Count => 7;

    public KeyValuePair<string, object?> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object?>(nameof(_httpContext.Request.Method), _httpContext.Request.Method),
        1 => new KeyValuePair<string, object?>(nameof(_httpContext.Request.Scheme), _httpContext.Request.Scheme),
        2 => new KeyValuePair<string, object?>(nameof(_httpContext.Request.Host), _httpContext.Request.Host),
        3 => new KeyValuePair<string, object?>(nameof(_httpContext.Request.PathBase), _httpContext.Request.PathBase),
        4 => new KeyValuePair<string, object?>(nameof(_httpContext.Request.Path), _httpContext.Request.Path),
        5 => new KeyValuePair<string, object?>(nameof(_httpContext.Response.StatusCode), _httpContext.Response.StatusCode),
        6 => new KeyValuePair<string, object?>("{OriginalFormat}", OriginalFormat),
        _ => throw new ArgumentOutOfRangeException(nameof(index)),
    };

    public HostingRequestUnhandledLog(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    public override string ToString()
    {
        if (_cachedToString == null)
        {
            var request = _httpContext.Request;
            var response = _httpContext.Response;
            _cachedToString = $"Request reached the end of the middleware pipeline without being handled by application code. Request path: {request.Method} {request.Scheme}://{request.Host}{request.PathBase}{request.Path}, Response status code: {response.StatusCode}";
        }

        return _cachedToString;
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
