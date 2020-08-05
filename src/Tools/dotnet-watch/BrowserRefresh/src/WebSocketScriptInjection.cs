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
    public class WebSocketScriptInjection
    {
        private const string BodyMarker = "</body>";

        private readonly byte[] _bodyBytes = Encoding.UTF8.GetBytes(BodyMarker);
        private readonly byte[] _scriptInjectionBytes;

        public static WebSocketScriptInjection Instance { get; } = new WebSocketScriptInjection(
            GetWebSocketClientJavaScript(Environment.GetEnvironmentVariable("ASPNETCORE_AUTO_RELOAD_WS_ENDPOINT")));

        public WebSocketScriptInjection(string clientScript)
        {
            _scriptInjectionBytes = Encoding.UTF8.GetBytes(clientScript);
        }

        public bool TryInjectLiveReloadScript(Stream baseStream, byte[] buffer, int offset, int count)
        {
            var span = buffer.AsSpan(offset, count);
            var index = span.LastIndexOf(_bodyBytes);
            if (index == -1)
            {
                baseStream.Write(span);
                return false;
            }

            if (index > 0)
            {
                baseStream.Write(span.Slice(0, index));
                span = span[index..];
            }

            // Write the injected script
            baseStream.Write(_scriptInjectionBytes);

            // Write the rest of the buffer/HTML doc
            baseStream.Write(span);
            return true;
        }

        public async ValueTask<bool> TryInjectLiveReloadScriptAsync(Stream baseStream, byte[] buffer, int offset, int count)
        {
            var index = buffer.AsSpan(offset, count).LastIndexOf(_bodyBytes);
            if (index == -1)
            {
                await baseStream.WriteAsync(buffer, offset, count);
                return false;
            }

            var memory = buffer.AsMemory(offset, count);

            if (index > 0)
            {
                await baseStream.WriteAsync(memory.Slice(0, index));
                memory = memory[index..];
            }

            // Write the injected script
            await baseStream.WriteAsync(_scriptInjectionBytes);

            // Write the rest of the buffer/HTML doc
            await baseStream.WriteAsync(memory);
            return true;
        }

        internal static string GetWebSocketClientJavaScript(string? hostString)
        {
            if (string.IsNullOrEmpty(hostString))
            {
                throw new InvalidOperationException("We expect ASPNETCORE_AUTO_RELOAD_WS_ENDPOINT to be specified.");
            }

            var jsFileName = "Microsoft.AspNetCore.Watch.BrowserRefresh.WebSocketScriptInjection.js";
            using var reader = new StreamReader(typeof(WebSocketScriptInjection).Assembly.GetManifestResourceStream(jsFileName)!);
            var script = reader.ReadToEnd().Replace("{{hostString}}", hostString);

            return $"<script>{script}</script>";
        }
    }
}
