// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Playwright;

namespace Wasm.Performance.Driver;

public class Program
{
    private const bool RunHeadless = true;

    internal static TaskCompletionSource<BenchmarkResult> BenchmarkResultTask;

    public static async Task<int> Main(string[] args)
    {
        // This cancellation token manages the timeout for the stress run.
        // By default the driver executes and reports a single Benchmark run. For stress runs,
        // we'll pass in the duration to execute the runs in seconds. This will cause this driver
        // to repeat executions for the duration specified.
        var stressRunCancellation = CancellationToken.None;
        var isStressRun = false;
        if (args.Length > 0)
        {
            if (!int.TryParse(args[0], out var stressRunSeconds))
            {
                Console.Error.WriteLine("Usage Driver <stress-run-duration-seconds>");
                return 1;
            }

            if (stressRunSeconds < 0)
            {
                Console.Error.WriteLine("Stress run duration must be a positive integer.");
                return 1;
            }
            else if (stressRunSeconds > 0)
            {
                isStressRun = true;

                var stressRunDuration = TimeSpan.FromSeconds(stressRunSeconds);
                Console.WriteLine($"Stress run duration: {stressRunDuration}.");
                stressRunCancellation = new CancellationTokenSource(stressRunDuration).Token;
            }
        }

        // This write is required for the benchmarking infrastructure.
        Console.WriteLine("Application started.");

        var browserArgs = new List<string>();
        if (isStressRun)
        {
            browserArgs.Add("--enable-precise-memory-info");
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = RunHeadless,
            Args = browserArgs,
        });
        using var testApp = StartTestApp();
        using var benchmarkReceiver = StartBenchmarkResultReceiver();
        var testAppUrl = GetListeningUrl(testApp);
        if (isStressRun)
        {
            testAppUrl += "/stress.html";
        }

        var receiverUrl = GetListeningUrl(benchmarkReceiver);
        Console.WriteLine($"Test app listening at {testAppUrl}.");

        var firstRun = true;
        var timeForEachRun = TimeSpan.FromMinutes(3);

        var launchUrl = $"{testAppUrl}?resultsUrl={UrlEncoder.Default.Encode(receiverUrl)}#automated";
        var page = await browser.NewPageAsync();
        await page.GotoAsync(launchUrl);
        page.Console += WriteBrowserConsoleMessage;

        do
        {
            BenchmarkResultTask = new TaskCompletionSource<BenchmarkResult>();
            using var runCancellationToken = new CancellationTokenSource(timeForEachRun);
            using var registration = runCancellationToken.Token.Register(async () =>
            {
                var exceptionMessage = $"Timed out after {timeForEachRun}.";
                try
                {
                    var innerHtml = await page.GetAttributeAsync(":first-child", "innerHTML");
                    exceptionMessage += Environment.NewLine + "Browser state: " + Environment.NewLine + innerHtml;
                }
                catch
                {
                    // Do nothing;
                }
                BenchmarkResultTask.TrySetException(new TimeoutException(exceptionMessage));
            });

            var results = await BenchmarkResultTask.Task;

            FormatAsBenchmarksOutput(results,
                includeMetadata: firstRun,
                isStressRun: isStressRun);

            if (!isStressRun)
            {
                PrettyPrint(results);
            }

            firstRun = false;
        } while (isStressRun && !stressRunCancellation.IsCancellationRequested);

        Console.WriteLine("Done executing benchmark");
        return 0;
    }

    private static void WriteBrowserConsoleMessage(object sender, IConsoleMessage message)
    {
        Console.WriteLine($"[Browser Log]: {message.Text}");
    }

    private static void FormatAsBenchmarksOutput(BenchmarkResult benchmarkResult, bool includeMetadata, bool isStressRun)
    {
        // Sample of the the format: https://github.com/aspnet/Benchmarks/blob/e55f9e0312a7dd019d1268c1a547d1863f0c7237/src/Benchmarks/Program.cs#L51-L67
        var output = new BenchmarkOutput();

        if (benchmarkResult.DownloadSize != null)
        {
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
        }

        if (benchmarkResult.WasmMemory != null)
        {
            output.Metadata.Add(new BenchmarkMetadata
            {
                Source = "BlazorWasm",
                Name = "blazorwasm/wasm-memory",
                ShortDescription = "Memory (KB)",
                LongDescription = "WASM reported memory (KB)",
                Format = "n2",
            });

            output.Measurements.Add(new BenchmarkMeasurement
            {
                Timestamp = DateTime.UtcNow,
                Name = "blazorwasm/wasm-memory",
                Value = ((float)benchmarkResult.WasmMemory) / 1024,
            });

            output.Metadata.Add(new BenchmarkMetadata
            {
                Source = "BlazorWasm",
                Name = "blazorwasm/js-usedjsheapsize",
                ShortDescription = "UsedJSHeapSize",
                LongDescription = "JS used heap size"
            });

            output.Measurements.Add(new BenchmarkMeasurement
            {
                Timestamp = DateTime.UtcNow,
                Name = "blazorwasm/js-usedjsheapsize",
                Value = benchmarkResult.UsedJSHeapSize,
            });

            output.Metadata.Add(new BenchmarkMetadata
            {
                Source = "BlazorWasm",
                Name = "blazorwasm/js-totaljsheapsize",
                ShortDescription = "TotalJSHeapSize",
                LongDescription = "JS total heap size"
            });

            output.Measurements.Add(new BenchmarkMeasurement
            {
                Timestamp = DateTime.UtcNow,
                Name = "blazorwasm/js-totaljsheapsize",
                Value = benchmarkResult.TotalJSHeapSize,
            });
        }

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

        if (!includeMetadata)
        {
            output.Metadata.Clear();
        }

        if (isStressRun)
        {
            output.Measurements.Add(new BenchmarkMeasurement
            {
                Timestamp = DateTime.UtcNow,
                Name = "$$Delimiter$$",
            });
        }

        var builder = new StringBuilder();
        builder.AppendLine("#StartJobStatistics");
        builder.AppendLine(JsonSerializer.Serialize(output));
        builder.AppendLine("#EndJobStatistics");

        Console.WriteLine(builder);
    }

    static void PrettyPrint(BenchmarkResult benchmarkResult)
    {
        Console.WriteLine();
        Console.WriteLine($"Download size: {(benchmarkResult.DownloadSize / 1024)}kb.");
        Console.WriteLine("| Name | Description | Duration | NumExecutions | ");
        Console.WriteLine("--------------------------");
        foreach (var result in benchmarkResult.ScenarioResults)
        {
            Console.WriteLine($"| {result.Descriptor.Name} | {result.Name} | {result.Duration} | {result.NumExecutions} |");
        }
    }

    static WebApplication StartTestApp()
    {
        string[] args = ["--urls", "http://127.0.0.1:0"];
        var app = WebApplication.Create(args);
        app.MapStaticAssets();
        app.MapFallbackToFile("index.html");

        RunInBackgroundThread(app.Start);
        return app;
    }

    static IHost StartBenchmarkResultReceiver()
    {
        string[] args = ["--urls", "http://127.0.0.1:0"];

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

        if (!isDone.WaitOne(TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException("Timed out waiting to start the host");
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
