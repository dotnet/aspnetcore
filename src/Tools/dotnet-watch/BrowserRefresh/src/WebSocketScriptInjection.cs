// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
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

        public bool TryInjectLiveReloadScript(Stream baseStream, ReadOnlySpan<byte> buffer)
        {
            var index = buffer.LastIndexOf(_bodyBytes);
            if (index == -1)
            {
                baseStream.Write(buffer);
                return false;
            }

            if (index > 0)
            {
                baseStream.Write(buffer.Slice(0, index));
                buffer = buffer[index..];
            }

            // Write the injected script
            baseStream.Write(_scriptInjectionBytes);

            // Write the rest of the buffer/HTML doc
            baseStream.Write(buffer);
            return true;
        }

        public async ValueTask<bool> TryInjectLiveReloadScriptAsync(Stream baseStream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) 
        {
            var index = buffer.Span.LastIndexOf(_bodyBytes);
            if (index == -1)
            {
                await baseStream.WriteAsync(buffer, cancellationToken);
                return false;
            }

            if (index > 0)
            {
                await baseStream.WriteAsync(buffer.Slice(0, index), cancellationToken);
                buffer = buffer[index..];
            }

            // Write the injected script
            await baseStream.WriteAsync(_scriptInjectionBytes, cancellationToken);

            // Write the rest of the buffer/HTML doc
            await baseStream.WriteAsync(buffer, cancellationToken);
            return true;
        }

        internal static string GetWebSocketClientJavaScript(string? hostString)
        {
            var jsFileName = "Microsoft.AspNetCore.Watch.BrowserRefresh.WebSocketScriptInjection.js";
            using var reader = new StreamReader(typeof(WebSocketScriptInjection).Assembly.GetManifestResourceStream(jsFileName)!);
            var script = reader.ReadToEnd().Replace("{{hostString}}", hostString);

            return $"<script>{script}</script>";
        }
    }
}
