// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    /// <summary>
    /// mod_rewrite lookups for specific string constants.
    /// </summary>
    public static class ServerVariables
    {
        public static HashSet<string> ValidServerVariables = new HashSet<string>()
        {
            "HTTP_ACCEPT",
            "HTTP_COOKIE",
            "HTTP_FORWARDED",
            "HTTP_HOST",
            "HTTP_PROXY_CONNECTION",
            "HTTP_REFERER",
            "HTTP_USER_AGENT",
            "AUTH_TYPE",
            "CONN_REMOTE_ADDR",
            "CONTEXT_PREFIX",
            "CONTEXT_DOCUMENT_ROOT",
            "IPV6",
            "PATH_INFO",
            "QUERY_STRING",
            "REMOTE_ADDR",
            "REMOTE_HOST",
            "REMOTE_IDENT",
            "REMOTE_PORT",
            "REMOTE_USER",
            "REQUEST_METHOD",
            "SCRIPT_FILENAME",
            "DOCUMENT_ROOT",
            "SCRIPT_GROUP",
            "SCRIPT_USER",
            "SERVER_ADDR",
            "SERVER_ADMIN",
            "SERVER_NAME",
            "SERVER_PORT",
            "SERVER_PROTOCOL",
            "SERVER_SOFTWARE",
            "TIME_YEAR",
            "TIME_MON",
            "TIME_DAY",
            "TIME_HOUR",
            "TIME_MIN",
            "TIME_SEC",
            "TIME_WDAY",
            "TIME",
            "API_VERSION",
            "HTTPS",
            "IS_SUBREQ",
            "REQUEST_FILENAME",
            "REQUEST_SCHEME",
            "REQUEST_URI",
            "THE_REQUEST"

        };

        /// <summary>
        /// Translates mod_rewrite server variables strings to an enum of different server variables.
        /// </summary>
        /// <param name="variable">The server variable string.</param>
        /// <param name="context">The HttpContext context.</param>
        /// <returns>The appropriate enum if the server variable exists, else ServerVariable.None</returns>
        public static string Resolve(string variable, HttpContext context)
        {
            // TODO talk about perf here
            switch (variable)
            {
                case "HTTP_ACCEPT":
                    return context.Request.Headers[HeaderNames.Accept];
                case "HTTP_COOKIE":
                    return context.Request.Headers[HeaderNames.Cookie];
                case "HTTP_FORWARDED":
                    return context.Request.Headers["Forwarded"];
                case "HTTP_HOST":
                    return context.Request.Headers[HeaderNames.Host];
                case "HTTP_PROXY_CONNECTION":
                    return context.Request.Headers[HeaderNames.ProxyAuthenticate];
                case "HTTP_REFERER":
                    return context.Request.Headers[HeaderNames.Referer];
                case "HTTP_USER_AGENT":
                    return context.Request.Headers[HeaderNames.UserAgent];
                case "AUTH_TYPE":
                    throw new NotImplementedException();
                case "CONN_REMOTE_ADDR":
                    return context.Connection.RemoteIpAddress?.ToString();
                case "CONTEXT_PREFIX":
                    throw new NotImplementedException();
                case "CONTEXT_DOCUMENT_ROOT":
                    throw new NotImplementedException();
                case "IPV6":
                    return context.Connection.LocalIpAddress.AddressFamily == AddressFamily.InterNetworkV6 ? "on" : "off";
                case "PATH_INFO":
                    throw new NotImplementedException();
                case "QUERY_STRING":
                    return context.Request.QueryString.Value;
                case "REMOTE_ADDR":
                    return context.Connection.RemoteIpAddress?.ToString();
                case "REMOTE_HOST":
                    throw new NotImplementedException();
                case "REMOTE_IDENT":
                    throw new NotImplementedException();
                case "REMOTE_PORT":
                    return context.Connection.RemotePort.ToString(CultureInfo.InvariantCulture);
                case "REMOTE_USER":
                    throw new NotImplementedException();
                case "REQUEST_METHOD":
                    return context.Request.Method;
                case "SCRIPT_FILENAME":
                    throw new NotImplementedException();
                case "DOCUMENT_ROOT":
                    throw new NotImplementedException();
                case "SCRIPT_GROUP":
                    throw new NotImplementedException();
                case "SCRIPT_USER":
                    throw new NotImplementedException();
                case "SERVER_ADDR":
                    return context.Connection.LocalIpAddress?.ToString();
                case "SERVER_ADMIN":
                    throw new NotImplementedException();
                case "SERVER_NAME":
                    throw new NotImplementedException();
                case "SERVER_PORT":
                    return context.Connection.LocalPort.ToString(CultureInfo.InvariantCulture);
                case "SERVER_PROTOCOL":
                    return context.Features.Get<IHttpRequestFeature>()?.Protocol;
                case "SERVER_SOFTWARE":
                    throw new NotImplementedException();
                case "TIME_YEAR":
                    return DateTimeOffset.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
                case "TIME_MON":
                    return DateTimeOffset.UtcNow.Month.ToString(CultureInfo.InvariantCulture);
                case "TIME_DAY":
                    return DateTimeOffset.UtcNow.Day.ToString(CultureInfo.InvariantCulture);
                case "TIME_HOUR":
                    return DateTimeOffset.UtcNow.Hour.ToString(CultureInfo.InvariantCulture);
                case "TIME_MIN":
                    return DateTimeOffset.UtcNow.Minute.ToString(CultureInfo.InvariantCulture);
                case "TIME_SEC":
                    return DateTimeOffset.UtcNow.Second.ToString(CultureInfo.InvariantCulture);
                case "TIME_WDAY":
                    return  ((int) DateTimeOffset.UtcNow.DayOfWeek).ToString(CultureInfo.InvariantCulture);
                case "TIME":
                    return DateTimeOffset.UtcNow.ToString(CultureInfo.InvariantCulture);
                case "API_VERSION":
                    throw new NotImplementedException();
                case "HTTPS":
                    return context.Request.IsHttps ? "on" : "off";
                case "HTTP2":
                    return context.Request.Scheme == "http2" ? "on" : "off";
                case "IS_SUBREQ":
                    // TODO maybe can do this? context.Request.HttpContext ?
                    throw new NotImplementedException();
                case "REQUEST_FILENAME":
                    return context.Request.Path.Value.Substring(1);
                case "REQUEST_SCHEME":
                    return context.Request.Scheme;
                case "REQUEST_URI":
                    // TODO This isn't an ideal solution. What this assumes is that all conditions don't have a leading slash before it.
                    return context.Request.Path.Value.Substring(1);
                case "THE_REQUEST":
                    // TODO
                    throw new NotImplementedException();
                default:
                    return null;
            }
        }
    }
}
