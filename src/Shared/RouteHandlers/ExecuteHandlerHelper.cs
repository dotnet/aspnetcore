// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Microsoft.AspNetCore.Internal;

internal static class ExecuteHandlerHelper
{
    public static Task ExecuteReturnAsync(object obj, HttpContext httpContext, JsonSerializerOptions options)
    {
        // Terminal built ins
        if (obj is IResult result)
        {
            return result.ExecuteAsync(httpContext);
        }
        else if (obj is string stringValue)
        {
            SetPlaintextContentType(httpContext);
            return httpContext.Response.WriteAsync(stringValue);
        }
        else
        {
            // Otherwise, we JSON serialize when we reach the terminal state
            return HttpResponseJsonExtensions.WriteAsJsonAsync(httpContext.Response, obj, obj.GetType() ?? typeof(object), options, default);
        }
    }

    public static void SetPlaintextContentType(HttpContext httpContext)
    {
        httpContext.Response.ContentType ??= "text/plain; charset=utf-8";
    }
}
