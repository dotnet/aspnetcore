// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Tools.Dump;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class EventSourceTests : LoggedTest
    {
        private readonly TestEventListener _listener = new TestEventListener();

        public EventSourceTests()
        {
            _listener.EnableEvents(KestrelEventSource.Log, EventLevel.Verbose);
        }

        [Fact]
        public async Task EmitsConnectionStartAndStop()
        {
            string connectionId = null;
            string requestId = null;
            int port;

            using (var server = new TestServer(context =>
            {
                connectionId = context.Features.Get<IHttpConnectionFeature>().ConnectionId;
                requestId = context.TraceIdentifier;
                return Task.CompletedTask;
            }, new TestServiceContext(LoggerFactory)))
            {
                port = server.Port;
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll("GET / HTTP/1.1",
                        "Host:",
                        "",
                        "")
                        .DefaultTimeout();
                    await connection.Receive("HTTP/1.1 200");

                    var getItems = typeof(ThreadPool).GetMethod("GetQueuedWorkItemsForDebugger", BindingFlags.Static | BindingFlags.NonPublic);

                    var queuedItems = (object[])getItems.Invoke(null, Array.Empty<object>());

                    ThreadPool.GetMaxThreads(out int maxWorkerThreads, out _);
                    ThreadPool.GetAvailableThreads(out int freeWorkerThreads, out _);
                    ThreadPool.GetMinThreads(out int minWorkerThreads, out _);

                    int busyWorkerThreads = maxWorkerThreads - freeWorkerThreads;

                    Logger.LogDebug("(WorkItems={workItems},Busy={busyWorkerThreads},Free={freeWorkerThreads},Min={minWorkerThreads},Max={maxWorkerThreads})", queuedItems.Length, busyWorkerThreads, freeWorkerThreads, minWorkerThreads, maxWorkerThreads);

                    if (busyWorkerThreads > minWorkerThreads)
                    {
                        await DumpThreadPoolStacks();
                    }
                }
            }

            // capture list here as other tests executing in parallel may log events
            Assert.NotNull(connectionId);
            Assert.NotNull(requestId);

            var events = _listener.EventData.Where(e => e != null && GetProperty(e, "connectionId") == connectionId).ToList();

            {
                var start = Assert.Single(events, e => e.EventName == "ConnectionStart");
                Assert.All(new[] { "connectionId", "remoteEndPoint", "localEndPoint" }, p => Assert.Contains(p, start.PayloadNames));
                Assert.Equal($"127.0.0.1:{port}", GetProperty(start, "localEndPoint"));
            }
            {
                var stop = Assert.Single(events, e => e.EventName == "ConnectionStop");
                Assert.All(new[] { "connectionId" }, p => Assert.Contains(p, stop.PayloadNames));
                Assert.Same(KestrelEventSource.Log, stop.EventSource);
            }
            {
                var requestStart = Assert.Single(events, e => e.EventName == "RequestStart");
                Assert.All(new[] { "connectionId", "requestId" }, p => Assert.Contains(p, requestStart.PayloadNames));
                Assert.Equal(requestId, GetProperty(requestStart, "requestId"));
                Assert.Same(KestrelEventSource.Log, requestStart.EventSource);
            }
            {
                var requestStop = Assert.Single(events, e => e.EventName == "RequestStop");
                Assert.All(new[] { "connectionId", "requestId" }, p => Assert.Contains(p, requestStop.PayloadNames));
                Assert.Equal(requestId, GetProperty(requestStop, "requestId"));
                Assert.Same(KestrelEventSource.Log, requestStop.EventSource);
            }
        }

        private async Task DumpThreadPoolStacks()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var path = Path.Combine(ResolvedLogOutputDirectory, ResolvedTestMethodName + ".dmp");

            var process = Process.GetCurrentProcess();

            await Dumper.CollectDumpAsync(process, path);

            var pid = process.Id;

            var sb = new StringBuilder();

            using (var dataTarget = DataTarget.AttachToProcess(pid, 5000, AttachFlag.Passive))
            {
                var runtime = dataTarget.ClrVersions[0].CreateRuntime();

                var threadPoolThreads = runtime.Threads.Where(t => t.IsThreadpoolWorker).ToList();

                sb.Append($"\nThreadPool Threads: {threadPoolThreads.Count}\n");

                foreach (var t in threadPoolThreads)
                {
                    if (!t.IsThreadpoolWorker)
                    {
                        continue;
                    }

                    // id
                    // stacktrace
                    var stackTrace = string.Join("\n", t.StackTrace.Select(f => f.ToString()));
                    sb.Append("\n====================================\n");
                    sb.Append($"Thread ID: {t.ManagedThreadId}\n");

                    if (t.StackTrace.Count == 0)
                    {
                        sb.Append("No stack\n");
                    }
                    else
                    {
                        sb.Append(stackTrace + "\n");
                    }
                    sb.Append("====================================\n");
                }
            }

            Logger.LogDebug(sb.ToString());
        }

        private string GetProperty(EventWrittenEventArgs data, string propName)
            => data.Payload[data.PayloadNames.IndexOf(propName)] as string;

        private class TestEventListener : EventListener
        {
            private volatile bool _disposed;
            private ConcurrentQueue<EventWrittenEventArgs> _events = new ConcurrentQueue<EventWrittenEventArgs>();

            public IEnumerable<EventWrittenEventArgs> EventData => _events;

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (!_disposed)
                {
                    _events.Enqueue(eventData);
                }
            }

            public override void Dispose()
            {
                _disposed = true;
                base.Dispose();
            }
        }

        public override void Dispose()
        {
            _listener.Dispose();
            base.Dispose();
        }
    }
}
