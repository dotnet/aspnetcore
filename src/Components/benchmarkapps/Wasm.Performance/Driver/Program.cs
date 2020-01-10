// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using DevHostServerProgram = Microsoft.AspNetCore.Blazor.DevServer.Server.Program;

namespace Wasm.Performance.Driver
{
    public class Program
    {
        static readonly TimeSpan Timeout = TimeSpan.FromMinutes(3);
        static TaskCompletionSource<List<BenchmarkResult>> benchmarkResult = new TaskCompletionSource<List<BenchmarkResult>>();

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

            var results = await benchmarkResult.Task;
            FormatAsBenchmarksOutput(results);

            Console.WriteLine("Done executing benchmark");
            return 0;
        }

        internal static void SetBenchmarkResult(List<BenchmarkResult> result)
        {
            benchmarkResult.TrySetResult(result);
        }

        private static void FormatAsBenchmarksOutput(List<BenchmarkResult> results)
        {
            // Sample of the the format: https://github.com/aspnet/Benchmarks/blob/e55f9e0312a7dd019d1268c1a547d1863f0c7237/src/Benchmarks/Program.cs#L51-L67
            var output = new BenchmarkOutput();
            foreach (var result in results)
            {
                output.Metadata.Add(new BenchmarkMetadata
                {
                    Source = "BlazorWasm",
                    Name = result.Name,
                    ShortDescription = $"{result.Name} Duration",
                    LongDescription = $"{result.Name} Duration",
                    Format = "n2"
                });

                output.Measurements.Add(new BenchmarkMeasurement
                {
                    Timestamp = DateTime.UtcNow,
                    Name = result.Name,
                    Value = result.Duration,
                });
            }

            // Statistics about publish sizes
            output.Metadata.Add(new BenchmarkMetadata
            {
                Source = "BlazorWasm",
                Name = "Publish size",
                ShortDescription = "Publish size (KB)",
                LongDescription = "Publish size (KB)",
                Format = "n2",
            });

            var testAssembly = typeof(TestApp.Startup).Assembly;
            var testAssemblyLocation = new FileInfo(testAssembly.Location);
            var testApp = new DirectoryInfo(Path.Combine(
                testAssemblyLocation.Directory.FullName,
                testAssembly.GetName().Name));

            output.Measurements.Add(new BenchmarkMeasurement
            {
                Timestamp = DateTime.UtcNow,
                Name = "Publish size",
                Value = GetDirectorySize(testApp) / 1024,
            });

            output.Metadata.Add(new BenchmarkMetadata
            {
                Source = "BlazorWasm",
                Name = "Publish size (compressed)",
                ShortDescription = "Publish size  compressed app (KB)",
                LongDescription = "Publish size - compressed app (KB)",
                Format = "n2",
            });

            var gzip = new FileInfo(Path.Combine(
                testAssemblyLocation.Directory.FullName,
                $"{testAssembly.GetName().Name}.gzip"));

            output.Measurements.Add(new BenchmarkMeasurement
            {
                Timestamp = DateTime.UtcNow,
                Name = "Publish size (compressed)",
                Value = (gzip.Exists ? gzip.Length : 0) / 1024,
            });

            Console.WriteLine("#StartJobStatistics");
            Console.WriteLine(JsonSerializer.Serialize(output));
            Console.WriteLine("#EndJobStatistics");
        }

        static IHost StartTestApp()
        {
            var args = new[]
            {
                "--urls", "http://127.0.0.1:0",
                "--applicationpath", typeof(TestApp.Startup).Assembly.Location,
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

        static long GetDirectorySize(DirectoryInfo directory)
        {
            // This can happen if you run the app without publishing it.
            if (!directory.Exists)
            {
                return 0;
            }

            long size = 0;
            foreach (var item in directory.EnumerateFileSystemInfos())
            {
                if (item is FileInfo fileInfo)
                {
                    size += fileInfo.Length;
                }
                else if (item is DirectoryInfo directoryInfo)
                {
                    size += GetDirectorySize(directoryInfo);
                }
            }

            return size;
        }
    }
}
