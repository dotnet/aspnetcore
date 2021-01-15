// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Watch.BrowserRefresh
{
    /// <summary>
    /// Responds with the contennts of WebSocketScriptInjection.js with the stub WebSocket url replaced by the
    /// one specified by the launching app.
    /// </summary>
    public sealed class BrowserScriptMiddleware
    {
        private readonly byte[] _scriptBytes;
        private readonly string _contentLength;

        public BrowserScriptMiddleware(RequestDelegate next)
            : this(Environment.GetEnvironmentVariable("ASPNETCORE_AUTO_RELOAD_WS_ENDPOINT")!)
        {
        }

        internal BrowserScriptMiddleware(string webSocketUrl)
        {
            _scriptBytes = GetWebSocketClientJavaScript(webSocketUrl);
            _contentLength = _scriptBytes.Length.ToString(CultureInfo.InvariantCulture);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.Headers["Cache-Control"] = "no-store";
            context.Response.Headers["Content-Length"] = _contentLength;
            context.Response.Headers["Content-Type"] = "application/javascript; charset=utf-8";

            await context.Response.Body.WriteAsync(_scriptBytes.AsMemory(), context.RequestAborted);
        }

        internal static byte[] GetWebSocketClientJavaScript(string hostString)
        {
            var jsFileName = "Microsoft.AspNetCore.Watch.BrowserRefresh.WebSocketScriptInjection.js";
            using var reader = new StreamReader(typeof(WebSocketScriptInjection).Assembly.GetManifestResourceStream(jsFileName)!);
            var script = reader.ReadToEnd().Replace("{{hostString}}", hostString);

            return Encoding.UTF8.GetBytes(script);
        }
    }
}
