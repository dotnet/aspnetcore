// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json;

namespace Microsoft.AspNetCore.Internal;

internal static class ExecuteHandlerHelper
{
    public static Task ExecuteReturnAsync(object obj, HttpContext httpContext, JsonSerializerOptions options, JsonTypeInfo<object> jsonTypeInfo)
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
            return WriteJsonResponseAsync(httpContext.Response, obj, options, jsonTypeInfo);
        }
    }

    public static void SetPlaintextContentType(HttpContext httpContext)
    {
        httpContext.Response.ContentType ??= "text/plain; charset=utf-8";
    }

    public static Task WriteJsonResponseAsync<T>(HttpResponse response, T? value, JsonSerializerOptions options, JsonTypeInfo<T> jsonTypeInfo)
    {
        var runtimeType = value?.GetType();

        if (jsonTypeInfo.IsValid(runtimeType))
        {
            // In this case the polymorphism is not
            // relevant for us and will be handled by STJ, if needed.
            return HttpResponseJsonExtensions.WriteAsJsonAsync(response, value!, jsonTypeInfo, default);
        }

        // Call WriteAsJsonAsync() with the runtime type to serialize the runtime type rather than the declared type
        // and avoid source generators issues.
        // https://github.com/dotnet/aspnetcore/issues/43894
        // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-polymorphism
        var runtimeTypeInfo = options.GetTypeInfo(runtimeType);
        return HttpResponseJsonExtensions.WriteAsJsonAsync(response, value!, runtimeTypeInfo, default);
    }
}
