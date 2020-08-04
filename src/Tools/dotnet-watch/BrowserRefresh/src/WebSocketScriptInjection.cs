// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Watch.BrowserRefresh
{
    /// <summary>
    /// Helper class that handles the HTML injection into
    /// a string or byte array.
    /// </summary>
    public static class WebSocketScriptInjection
    {
        private const string BodyMarker = "</body>";

        private static readonly byte[] _bodyBytes = Encoding.UTF8.GetBytes(BodyMarker);
        private static readonly byte[] _scriptInjectionBytes = Encoding.UTF8.GetBytes(GetWebSocketClientJavaScript());

        public static async ValueTask<bool> TryInjectLiveReloadScriptAsync(byte[] buffer, int offset, int count, Stream baseStream)
        {
            var index = buffer.AsSpan(offset, count).LastIndexOf(_bodyBytes);
            if (index == -1)
            {
                await baseStream.WriteAsync(buffer, 0, buffer.Length);
                return false;
            }

            if (index > 0)
            {
                await baseStream.WriteAsync(buffer, offset, index - 1);
            }

            // Write the injected script
            await baseStream.WriteAsync(_scriptInjectionBytes);

            // Write the rest of the buffer/HTML doc
            await baseStream.WriteAsync(buffer, index, buffer.Length - index);
            return true;
        }

        private static string GetWebSocketClientJavaScript()
        {
            var hostString = Environment.GetEnvironmentVariable("ASPNETCORE_AUTO_RELOAD_WS_ENDPOINT");
            var jsFileName = "Microsoft.AspNetCore.Watch.BrowserRefresh.WebSocketScriptInjection.js";
            using var reader = new StreamReader(typeof(WebSocketScriptInjection).Assembly.GetManifestResourceStream(jsFileName)!);
            var script = reader.ReadToEnd().Replace("{{hostString}}", hostString);

            return $"<script>{script}</script>";
        }
    }
}
