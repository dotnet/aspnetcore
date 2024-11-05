// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener;

internal static class Utilities
{
    internal static readonly int WriteRetryLimit = 1000;
    internal static readonly byte[] WriteBuffer = new byte[1024 * 1024];

    // When tests projects are run in parallel, overlapping port ranges can cause a race condition when looking for free
    // ports during dynamic port allocation.
    private const int BasePort = 8001;
    private const int MaxPort = 11000;
    private static int NextPort = BasePort;
    private static object PortLock = new object();

    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    internal static HttpSysListener CreateHttpServer(out string baseAddress)
    {
        string root;
        return CreateDynamicHttpServer(string.Empty, out root, out baseAddress);
    }

    internal static HttpSysListener CreateHttpServerReturnRoot(string path, out string root)
    {
        string baseAddress;
        return CreateDynamicHttpServer(path, out root, out baseAddress);
    }

    internal static HttpSysListener CreateDynamicHttpServer(string basePath, out string root, out string baseAddress)
    {
        lock (PortLock)
        {
            while (NextPort < MaxPort)
            {
                var port = NextPort++;
                var prefix = UrlPrefix.Create("http", "localhost", port, basePath);
                root = prefix.Scheme + "://" + prefix.Host + ":" + prefix.Port;
                baseAddress = prefix.ToString();
                var options = new HttpSysOptions();
                options.UrlPrefixes.Add(prefix);
                options.RequestQueueName = prefix.Port; // Convention for use with CreateServerOnExistingQueue
                var listener = new HttpSysListener(options, new LoggerFactory());
                try
                {
                    listener.Start();
                    return listener;
                }
                catch (HttpSysException ex)
                {
                    listener.Dispose();
                    if (ex.ErrorCode != ErrorCodes.ERROR_ALREADY_EXISTS
                        && ex.ErrorCode != ErrorCodes.ERROR_SHARING_VIOLATION
                        && ex.ErrorCode != ErrorCodes.ERROR_ACCESS_DENIED)
                    {
                        throw;
                    }
                }
            }
            NextPort = BasePort;
        }
        throw new Exception("Failed to locate a free port.");
    }

    internal static HttpSysListener CreateHttpsServer()
    {
        return CreateServer("https", "localhost", 9090, string.Empty);
    }

    internal static HttpSysListener CreateServer(string scheme, string host, int port, string path)
    {
        var listener = new HttpSysListener(new HttpSysOptions(), new LoggerFactory());
        listener.Options.UrlPrefixes.Add(UrlPrefix.Create(scheme, host, port, path));
        listener.Start();
        return listener;
    }

    internal static HttpSysListener CreateServer(Action<HttpSysOptions> configureOptions)
    {
        var options = new HttpSysOptions();
        configureOptions(options);
        var listener = new HttpSysListener(options, new LoggerFactory());
        listener.Start();
        return listener;
    }

    internal static HttpSysListener CreateServerOnExistingQueue(string requestQueueName)
    {
        return CreateServer(options =>
        {
            options.RequestQueueName = requestQueueName;
            options.RequestQueueMode = RequestQueueMode.Attach;
        });
    }

    /// <summary>
    /// AcceptAsync extension with timeout. This extension should be used in all tests to prevent
    /// unexpected hangs when a request does not arrive.
    /// </summary>
    internal static async Task<RequestContext> AcceptAsync(this HttpSysListener server, TimeSpan timeout)
    {
        var factory = new TestRequestContextFactory(server);
        using var acceptContext = new AsyncAcceptContext(server, factory, server.Logger);

        async Task<RequestContext> AcceptAsync()
        {
            while (true)
            {
                var requestContext = await server.AcceptAsync(acceptContext);

                if (server.ValidateRequest(requestContext))
                {
                    requestContext.InitializeFeatures();
                    return requestContext;
                }

                requestContext.ReleasePins();
                requestContext.Dispose();
            }
        }

        var acceptTask = AcceptAsync();
        var completedTask = await Task.WhenAny(acceptTask, Task.Delay(timeout));

        if (completedTask == acceptTask)
        {
            return await acceptTask;
        }
        else
        {
            server.Dispose();
            throw new TimeoutException("AcceptAsync has timed out.");
        }
    }

    // Fail if the given response task completes before the given accept task.
    internal static async Task<RequestContext> Before<T>(this Task<RequestContext> acceptTask, Task<T> responseTask)
    {
        var completedTask = await Task.WhenAny(acceptTask, responseTask);

        if (completedTask == acceptTask)
        {
            return await acceptTask;
        }
        else
        {
            var response = await responseTask;
            throw new InvalidOperationException("The response completed prematurely: " + response.ToString());
        }
    }

    private class TestRequestContextFactory : IRequestContextFactory
    {
        private readonly HttpSysListener _server;

        public TestRequestContextFactory(HttpSysListener server)
        {
            _server = server;
        }

        public RequestContext CreateRequestContext(uint? bufferSize, ulong requestId)
        {
            return new RequestContext(_server, bufferSize, requestId);
        }
    }
}
