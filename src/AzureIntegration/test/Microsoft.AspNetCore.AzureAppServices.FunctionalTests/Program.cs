using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Microsoft.AspNetCore.AzureAppServices.FunctionalTests
{
    public static class Program
    {
        internal static readonly string ArtifactsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "artifacts");

        public static int Main(string[] args)
        {
            var loggerFactory = CreateLoggerFactory();
            var logger = loggerFactory.CreateLogger(nameof(Program));

            var azure = new AzureFixture(loggerFactory);

            var cancellationTokenSource = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = false;
                cancellationTokenSource.Cancel();
            };

            var failedTests = new List<string>();
            try
            {
                var testCases = GenerateTestCases(azure, logger);
                var runningTests = new List<(string Name, Task Task)>();
                foreach (var testCase in testCases)
                {
                    logger.LogInformation("Starting {TestCase}", testCase.Name);
                    runningTests.Add((testCase.Name, testCase.Execute()));
                }
                foreach (var runningTest in runningTests)
                {
                    try
                    {
                        runningTest.Task.Wait(cancellationTokenSource.Token);
                    }
                    catch
                    {
                        failedTests.Add(runningTest.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error executing tests");
                return 1;
            }
            finally
            {
                azure.Dispose();
            }
            if (failedTests.Any())
            {
                logger.LogCritical("Tests failed: {Tests}", string.Join(",", failedTests));
            }
            return failedTests.Count;
        }

        private static ILoggerFactory CreateLoggerFactory()
        {
            var serilogger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(
                    new PerLoggerSink(
                        (key, config) => config.File(
                            Path.Combine(ArtifactsPath, "logs", key + ".log"),
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{SourceContext}] [{Level}] {Message}{NewLine}{Exception}",
                            flushToDiskInterval: TimeSpan.FromSeconds(1), shared: true)))
                .CreateLogger();

            var collection = new ServiceCollection();
            collection.AddLogging(
                builder =>
                    builder
                        .AddFilter<SerilogLoggerProvider>(null, LogLevel.Trace)
                        .AddConsole(options => options.IncludeScopes = true)
                        .AddProvider(new SerilogLoggerProvider(serilogger)));

            var loggerFactory = collection.BuildServiceProvider().GetService<ILoggerFactory>();
            return loggerFactory;
        }

        private static IEnumerable<(string Name, Func<Task> Execute)> GenerateTestCases(AzureFixture azureFixture, ILogger logger)
        {
            var deployMethods = new[]
            {
                WebAppDeploymentKind.Git,
                WebAppDeploymentKind.WebDeploy
            };

            var legacyTemplates = new (string Version, string RuntimeVersion, string Name, string Output)[]
            {
                ("1.0.5", "1.0.7", "web", "Hello World!"),
                ("1.0.5", "1.0.7", "mvc", "Learn how to build ASP.NET apps that can run anywhere"),
                ("1.0.6", "1.0.7", "web", "Hello World!"),
                ("1.0.6", "1.0.7", "mvc", "Learn how to build ASP.NET apps that can run anywhere"),
                ("1.1.2", "1.1.4", "web", "Hello World!"),
                ("1.1.2", "1.1.4", "mvc", "Learn how to build ASP.NET apps that can run anywhere"),
                ("1.1.3", "1.1.4", "web", "Hello World!"),
                ("1.1.3", "1.1.4", "mvc", "Learn how to build ASP.NET apps that can run anywhere"),
            };

            foreach (var template in legacyTemplates)
            {
                foreach (var webAppDeploymentKind in deployMethods)
                {
                    yield return RunTest(logger,
                        () => new TemplateFunctionalTests(azureFixture, logger).LegacyTemplateRuns(webAppDeploymentKind, template.Version, template.RuntimeVersion, template.Name, template.Output),
                        "LegacyTemplateRuns", webAppDeploymentKind, template.Version, template.Name);
                }
            }

            var frameworks = new[] { "2.0", "latest" };
            var templates = new(string Name, string Output)[]
            {
                ("web", "Hello World!"),
                ("mvc", "Learn how to build ASP.NET apps that can run anywhere"),
                ("razor", "Learn how to build ASP.NET apps that can run anywhere")
            };

            foreach (var framework in frameworks)
            {
                foreach (var template in templates)
                {
                    foreach (var webAppDeploymentKind in deployMethods)
                    {
                        yield return RunTest(logger,
                            () => new TemplateFunctionalTests(azureFixture, logger).TemplateRuns(webAppDeploymentKind, framework, template.Name, template.Output),
                            "TemplateRuns", webAppDeploymentKind, framework, template.Name);
                    }
                }
            }
        }

        private static (string, Func<Task>) RunTest(ILogger logger, Func<Task> func, params object[] nameParts)
        {
            var name = string.Join("_", nameParts);
            return (name, async () =>
                {
                    using (logger.BeginScope("Test: {TestName}", name))
                    {
                        try
                        {
                            await func();
                            logger.LogInformation("Test run successful");
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, "Error running test");
                            throw;
                        }
                    }
                });
        }
    }
}
