// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Shared;

internal static class HttpContextDebugFormatter
{
    public static string ResponseToString(HttpResponse response, string? reasonPhrase)
    {
        var sb = new StringBuilder();
        sb.Append(response.StatusCode);
        var resolvedReasonPhrase = ResolveReasonPhrase(response, reasonPhrase);
        if (!string.IsNullOrEmpty(resolvedReasonPhrase))
        {
            sb.Append(' ');
            sb.Append(resolvedReasonPhrase);
        }
        if (!string.IsNullOrEmpty(response.ContentType))
        {
            sb.Append(' ');
            sb.Append(response.ContentType);
        }
        return sb.ToString();
    }

    public static string RequestToString(HttpRequest request)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(request.Method))
        {
            sb.Append(request.Method);
            sb.Append(' ');
        }
        GetRequestUrl(sb, request, includeQueryString: true);
        if (!string.IsNullOrEmpty(request.Protocol))
        {
            sb.Append(' ');
            sb.Append(request.Protocol);
        }
        if (!string.IsNullOrEmpty(request.ContentType))
        {
            sb.Append(' ');
            sb.Append(request.ContentType);
        }
        return sb.ToString();
    }

    public static string ContextToString(HttpContext context, string? reasonPhrase)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(context.Request.Method))
        {
            sb.Append(context.Request.Method);
            sb.Append(' ');
        }
        GetRequestUrl(sb, context.Request, includeQueryString: false);
        if (!string.IsNullOrEmpty(context.Request.Protocol))
        {
            sb.Append(' ');
            sb.Append(context.Request.Protocol);
        }
        if (!string.IsNullOrEmpty(context.Request.ContentType))
        {
            sb.Append(' ');
            sb.Append(context.Request.ContentType);
        }
        sb.Append(' ');
        sb.Append(context.Response.StatusCode);
        var resolvedReasonPhrase = ResolveReasonPhrase(context.Response, reasonPhrase);
        if (!string.IsNullOrEmpty(resolvedReasonPhrase))
        {
            sb.Append(' ');
            sb.Append(resolvedReasonPhrase);
        }

        return sb.ToString();
    }

    private static string? ResolveReasonPhrase(HttpResponse response, string? reasonPhrase)
    {
        return response.HttpContext.Features.Get<IHttpResponseFeature>()?.ReasonPhrase ?? reasonPhrase;
    }

    private static void GetRequestUrl(StringBuilder sb, HttpRequest request, bool includeQueryString)
    {
        // The URL might be missing because the context was manually created in a test, e.g. new DefaultHttpContext()
        if (string.IsNullOrEmpty(request.Scheme) &&
            !request.Host.HasValue &&
            !request.PathBase.HasValue &&
            !request.Path.HasValue &&
            !request.QueryString.HasValue)
        {
            sb.Append("(unspecified)");
            return;
        }

        // If some parts of the URL are provided then default the significant parts to avoid a werid output.
        var scheme = request.Scheme;
        if (string.IsNullOrEmpty(scheme))
        {
            scheme = "(unspecified)";
        }
        var host = request.Host.Value;
        if (string.IsNullOrEmpty(host))
        {
            host = "(unspecified)";
        }

        sb.Append(CultureInfo.InvariantCulture, $"{scheme}://{host}{request.PathBase.Value}{request.Path.Value}{(includeQueryString ? request.QueryString.Value : string.Empty)}");
    }
}
