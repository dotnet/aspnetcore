// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.E2ETesting
{
    class SeleniumStandaloneServer
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
        private static Lazy<SeleniumStandaloneServer> _instance = new Lazy<SeleniumStandaloneServer>(() => new SeleniumStandaloneServer());

        public Uri Uri { get; }

        public static SeleniumStandaloneServer Instance => _instance.Value;

        private SeleniumStandaloneServer()
        {
            var port = FindAvailablePort();
            Uri = new UriBuilder("http", "localhost", port, "/wd/hub").Uri;

            var psi = new ProcessStartInfo
            {
                FileName = "npm",
                Arguments = $"run selenium-standalone start -- -- -port {port}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                psi.FileName = "cmd";
                psi.Arguments = $"/c npm {psi.Arguments}";
            }

            var process = Process.Start(psi);

            var builder = new StringBuilder();
            process.OutputDataReceived += LogOutput;
            process.ErrorDataReceived += LogOutput;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // The Selenium sever has to be up for the entirety of the tests and is only shutdown when the application (i.e. the test) exits.
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                if (!process.HasExited)
                {
                    process.KillTree(TimeSpan.FromSeconds(10));
                    process.Dispose();
                }
            };

            void LogOutput(object sender, DataReceivedEventArgs e)
            {
                lock (builder)
                {
                    builder.AppendLine(e.Data);
                }
            }

            var waitForStart = Task.Run(async () =>
            {
                var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(1),
                };

                var retries = 0;
                while (retries++ < 30)
                {
                    try
                    {
                        var responseTask = httpClient.GetAsync(Uri);

                        var response = await responseTask;
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            return;
                        }
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    await Task.Delay(1000);
                }

                throw new Exception("Failed to launch the server");
            });

            try
            {
                // Wait in intervals instead of indefinitely to prevent thread starvation.
                while (!waitForStart.TimeoutAfter(Timeout).Wait(1000))
                {
                }
            }
            catch (Exception ex)
            {
                string output;
                lock (builder)
                {
                    output = builder.ToString();
                }

                throw new InvalidOperationException($"Failed to start selenium sever. {System.Environment.NewLine}{output}", ex.GetBaseException());
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
