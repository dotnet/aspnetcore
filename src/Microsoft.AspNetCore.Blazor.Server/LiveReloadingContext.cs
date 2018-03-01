// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Blazor.Server
{
    internal class LiveReloadingContext
    {
        // Keep in sync with $(BlazorBuildCompletedSignalPath) in Blazor.MonoRuntime.props
        private const string BlazorBuildCompletedSignalFile = "__blazorBuildCompleted";

        // If some external automated process is writing multiple files to wwwroot,
        // you probably want to wait until they've all been written before reloading.
        // Pausing by 500 milliseconds is a crude effort - we might need a different
        // mechanism (e.g., waiting until writes have stopped by 500ms).
        private const int WebRootUpdateDelayMilliseconds = 500;
        private static byte[] _reloadMessage = Encoding.UTF8.GetBytes("reload");

        // If we don't hold references to them, then on Linux they get disposed.
        // This static would leak memory if you called UseBlazorLiveReloading continually
        // throughout the app lifetime, but the intended usage is just during init.
        private static readonly List<FileSystemWatcher> _pinnedWatchers = new List<FileSystemWatcher>();

        private readonly object _currentReloadListenerLock = new object();
        private CancellationTokenSource _currentReloadListener
            = new CancellationTokenSource();

        public void Attach(IApplicationBuilder applicationBuilder, BlazorConfig config)
        {
            CreateFileSystemWatchers(config);
            AddWebSocketsEndpoint(applicationBuilder, config.ReloadUri);
        }

        private void AddWebSocketsEndpoint(IApplicationBuilder applicationBuilder, string url)
        {
            applicationBuilder.UseWebSockets();
            applicationBuilder.Use((context, next) =>
            {
                if (!string.Equals(context.Request.Path, url, StringComparison.Ordinal))
                {
                    return next();
                }

                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    return context.Response.WriteAsync("This endpoint only accepts WebSockets connections.");
                }

                return HandleWebSocketRequest(
                    context.WebSockets.AcceptWebSocketAsync(),
                    context.RequestAborted);
            });
        }

        private async Task HandleWebSocketRequest(Task<WebSocket> webSocketTask, CancellationToken requestAbortedToken)
        {
            var webSocket = await webSocketTask;
            var reloadToken = _currentReloadListener.Token;

            // Wait until either we get a signal to trigger a reload, or the client disconnects
            // In either case we're done after that. It's the client's job to reload and start
            // a new live reloading context.
            try
            {
                var reloadOrRequestAbortedToken = CancellationTokenSource
                    .CreateLinkedTokenSource(reloadToken, requestAbortedToken)
                    .Token;
                await Task.Delay(-1, reloadOrRequestAbortedToken);
            }
            catch (TaskCanceledException)
            {
                if (reloadToken.IsCancellationRequested)
                {
                    await webSocket.SendAsync(
                        _reloadMessage,
                        WebSocketMessageType.Text,
                        true,
                        requestAbortedToken);
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Requested reload",
                        requestAbortedToken);
                }
            }
        }

        private void CreateFileSystemWatchers(BlazorConfig config)
        {
            // Watch for the "build completed" signal in the dist dir
            var distFileWatcher = new FileSystemWatcher(config.DistPath);
            distFileWatcher.Deleted += (sender, eventArgs) => {
                if (eventArgs.Name.Equals(BlazorBuildCompletedSignalFile, StringComparison.Ordinal))
                {
                    RequestReload(0);
                }
            };
            distFileWatcher.EnableRaisingEvents = true;
            _pinnedWatchers.Add(distFileWatcher);

            // If there's a WebRootPath, watch for any file modification there
            // WebRootPath is only used in dev builds, where we want to serve from wwwroot directly
            // without requiring the developer to rebuild after changing the static files there.
            // In production there's no need for it.
            if (!string.IsNullOrEmpty(config.WebRootPath))
            {
                var webRootWatcher = new FileSystemWatcher(config.WebRootPath);
                webRootWatcher.Deleted += (sender, evtArgs) => RequestReload(WebRootUpdateDelayMilliseconds);
                webRootWatcher.Created += (sender, evtArgs) => RequestReload(WebRootUpdateDelayMilliseconds);
                webRootWatcher.Changed += (sender, evtArgs) => RequestReload(WebRootUpdateDelayMilliseconds);
                webRootWatcher.Renamed += (sender, evtArgs) => RequestReload(WebRootUpdateDelayMilliseconds);
                webRootWatcher.EnableRaisingEvents = true;
                _pinnedWatchers.Add(webRootWatcher);
            }
        }

        private void RequestReload(int delayMilliseconds)
        {
            Task.Delay(delayMilliseconds).ContinueWith(_ =>
            {
                lock (_currentReloadListenerLock)
                {
                    // Lock just to be sure two threads don't assign different new CTSs, of which
                    // only one would later get cancelled.
                    _currentReloadListener.Cancel();
                    _currentReloadListener = new CancellationTokenSource();
                }
            });
        }
    }
}
