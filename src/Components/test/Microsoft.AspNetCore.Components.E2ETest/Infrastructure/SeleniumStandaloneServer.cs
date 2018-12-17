// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Internal;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure
{
    class SeleniumStandaloneServer
    {
        private static object _instanceCreationLock = new object();
        private static SeleniumStandaloneServer _instance;

        public Uri Uri { get; }

        public static SeleniumStandaloneServer Instance
        {
            get
            {
                lock (_instanceCreationLock)
                {
                    if (_instance == null)
                    {
                        _instance = new SeleniumStandaloneServer();
                    }
                }

                return _instance;
            }
        }

        private SeleniumStandaloneServer()
        {
            var port = FindAvailablePort();
            Uri = new UriBuilder("http", "localhost", port, "/wd/hub").Uri;

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "npm",
                Arguments = $"run selenium-standalone start -- -- -port {port}",
                UseShellExecute = true,
            });

            PollUntilProcessStarted();

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                if (!process.HasExited)
                {
                    process.KillTree(TimeSpan.FromSeconds(10));
                    process.Dispose();
                }
            };
        }

        private void PollUntilProcessStarted()
        {
            var timeoutAt = DateTime.Now.AddSeconds(30);
            Exception lastException = null;
            while (true)
            {
                if (DateTime.Now > timeoutAt)
                {
                    throw new TimeoutException($"The selenium server instance did not start accepting requests at {Uri} before the timeout occurred. The last exception was: {lastException?.ToString() ?? "NULL"}");
                }

                var httpClient = new HttpClient();
                try
                {
                    var timeoutAfter1Second = new CancellationTokenSource(3000);
                    var response = httpClient.GetAsync(
                        Uri, timeoutAfter1Second.Token).Result;
                    response.EnsureSuccessStatusCode();
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                Thread.Sleep(1000);
            }
        }

        static int FindAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);

            try
            {
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
