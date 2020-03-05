// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DevHostServerProgram = Microsoft.AspNetCore.Components.WebAssembly.DevServer.Server.Program;

namespace Wasm.Performance.Driver
{
    public class Program
    {
        static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);
        static TaskCompletionSource<BenchmarkResult> benchmarkResult = new TaskCompletionSource<BenchmarkResult>();

        public static async Task<int> Main(string[] args)
        {
            var seleniumPort = 4444;
            if (args.Length > 0)
            {
                if (!int.TryParse(args[0], out seleniumPort))
                {
                    Console.Error.WriteLine("Usage Driver <selenium-port>");
                    return 1;
                }
            }

            // This write is required for the benchmarking infrastructure.
            Console.WriteLine("Application started.");

            var cancellationToken = new CancellationTokenSource(Timeout);
            cancellationToken.Token.Register(() => benchmarkResult.TrySetException(new TimeoutException($"Timed out after {Timeout}")));

            using var browser = await Selenium.CreateBrowser(seleniumPort, cancellationToken.Token);
            using var testApp = StartTestApp();
            using var benchmarkReceiver = StartBenchmarkResultReceiver();

            var testAppUrl = GetListeningUrl(testApp);
            var receiverUrl = GetListeningUrl(benchmarkReceiver);

            Console.WriteLine($"Test app listening at {testAppUrl}.");

            var launchUrl = $"{testAppUrl}?resultsUrl={UrlEncoder.Default.Encode(receiverUrl)}#automated";
            browser.Url = launchUrl;
            browser.Navigate();

            FormatAsBenchmarksOutput(benchmarkResult.Task.Result);

            Console.WriteLine("Done executing benchmark");
            return 0;
        }

        internal static void SetBenchmarkResult(BenchmarkResult result)
        {
            benchmarkResult.TrySetResult(result);
        }

        private static void FormatAsBenchmarksOutput(BenchmarkResult benchmarkResult)
        {
            // Sample of the the format: https://github.com/aspnet/Benchmarks/blob/e55f9e0312a7dd019d1268c1a547d1863f0c7237/src/Benchmarks/Program.cs#L51-L67
            var output = new BenchmarkOutput();
            output.Metadata.Add(new BenchmarkMetadata
            {
                Source = "BlazorWasm",
                Name = "blazorwasm/download-size",
                ShortDescription = "Download size (KB)",
                LongDescription = "Download size (KB)",
                Format = "n2",
            });

            output.Measurements.Add(new BenchmarkMeasurement
            {
                Timestamp = DateTime.UtcNow,
                Name = "blazorwasm/download-size",
                Value = ((float)benchmarkResult.DownloadSize) / 1024,
            });

            // Information about the build that this was produced from
            output.Metadata.Add(new BenchmarkMetadata
            {
                Source = "BlazorWasm",
                Name = "blazorwasm/commit",
                ShortDescription = "Commit Hash",
            });

            output.Measurements.Add(new BenchmarkMeasurement
            {
                Timestamp = DateTime.UtcNow,
                Name = "blazorwasm/commit",
                Value = typeof(Program).Assembly
                    .GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(f => f.Key == "CommitHash")
                    ?.Value,
            });

            foreach (var result in benchmarkResult.ScenarioResults)
            {
                var scenarioName = result.Descriptor.Name;
                output.Metadata.Add(new BenchmarkMetadata
                {
                    Source = "BlazorWasm",
                    Name = scenarioName,
                    ShortDescription = result.Name,
                    LongDescription = result.Descriptor.Description,
                    Format = "n2"
                });

                output.Measurements.Add(new BenchmarkMeasurement
                {
                    Timestamp = DateTime.UtcNow,
                    Name = scenarioName,
                    Value = result.Duration,
                });
            }

            Console.WriteLine("#StartJobStatistics");
            Console.WriteLine(JsonSerializer.Serialize(output));
            Console.WriteLine("#EndJobStatistics");
        }

        static IHost StartTestApp()
        {
            var args = new[]
            {
                "--urls", "http://127.0.0.1:0",
                "--applicationpath", typeof(TestApp.Program).Assembly.Location,
#if DEBUG
                "--contentroot",
                Path.GetFullPath(typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .First(f => f.Key == "TestAppLocatiion")
                    .Value)
#endif
            };

            var host = DevHostServerProgram.BuildWebHost(args);
            RunInBackgroundThread(host.Start);
            return host;
        }

        static IHost StartBenchmarkResultReceiver()
        {
            var args = new[]
            {
                "--urls", "http://127.0.0.1:0",
            };

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(builder => builder.UseStartup<BenchmarkDriverStartup>())
                .Build();

            RunInBackgroundThread(host.Start);
            return host;
        }

        static void RunInBackgroundThread(Action action)
        {
            var isDone = new ManualResetEvent(false);

            ExceptionDispatchInfo edi = null;
            Task.Run(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    edi = ExceptionDispatchInfo.Capture(ex);
                }

                isDone.Set();
            });

            if (!isDone.WaitOne(Timeout))
            {
                throw new TimeoutException("Timed out waiting for: " + action);
            }

            if (edi != null)
            {
                throw edi.SourceException;
            }
        }

        static string GetListeningUrl(IHost testApp)
        {
            return testApp.Services.GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()
                .Addresses
                .First();
        }
    }
}
