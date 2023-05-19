// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Shared;

internal static class HttpContextDebugFormatter
{
    public static string ResponseToString(HttpResponse response, string? reasonPhrase)
    {
        var text = response.StatusCode.ToString(CultureInfo.InvariantCulture);
        var resolvedReasonPhrase = response.HttpContext.Features.Get<IHttpResponseFeature>()?.ReasonPhrase ?? reasonPhrase;
        if (!string.IsNullOrEmpty(resolvedReasonPhrase))
        {
            text += $" {resolvedReasonPhrase}";
        }
        if (!string.IsNullOrEmpty(response.ContentType))
        {
            text += $" {response.ContentType}";
        }
        return text;
    }

    public static string RequestToString(HttpRequest request, bool includeQueryString)
    {
        var text = $"{request.Method} {request.Scheme}://{request.Host.Value}{request.PathBase.Value}{request.Path.Value}{(includeQueryString ? request.QueryString.Value : string.Empty)} {request.Protocol}";
        if (!string.IsNullOrEmpty(request.ContentType))
        {
            text += $" {request.ContentType}";
        }
        return text;
    }

    public static string ContextToString(HttpContext context, string? reasonPhrase)
    {
        return $"Request = {RequestToString(context.Request, includeQueryString: false)}, Response = {ResponseToString(context.Response, reasonPhrase)}";
    }
}
