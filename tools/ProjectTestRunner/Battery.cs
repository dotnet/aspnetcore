using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json.Linq;
using ProjectTestRunner.HandlerResults;
using ProjectTestRunner.Handlers;
using ProjectTestRunner.Helpers;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ProjectTestRunner
{
    public class Battery
    {
        private static readonly IReadOnlyDictionary<string, IHandler> HandlerLookup = new Dictionary<string, IHandler>
        {
            { ExecuteHandler.Handler, new ExecuteHandler() },
            { TaskKillHandler.Handler, new TaskKillHandler() },
            { HttpRequestHandler.Handler, new HttpRequestHandler() },
            { FindProcessHandler.Handler, new FindProcessHandler() },
            { FileInspectHandler.Handler, new FileInspectHandler() },
            { DirectoryInspectHandler.Handler, new DirectoryInspectHandler() },
        };

        private static readonly string Creator;
        private static readonly string BasePath;

        static Battery()
        {
            string assemblyPath = typeof(Battery).GetTypeInfo().Assembly.CodeBase;
            Uri assemblyUri = new Uri(assemblyPath, UriKind.Absolute);
            assemblyPath = assemblyUri.LocalPath;
            BasePath = Path.GetDirectoryName(assemblyPath);

            Creator = Environment.GetEnvironmentVariable("CREATION_TEST_RUNNER");

            if (string.IsNullOrWhiteSpace(Creator))
            {
                Creator = "new";
            }

            Proc.Run("dotnet", $"--version").WaitForExit();

            Proc.Run("dotnet", $"{Creator} --debug:reinit").WaitForExit();
            Proc.Run("dotnet", $"{Creator}").WaitForExit();

            string templateFeedDirectory = FindTemplateFeedDirectory(BasePath);
            Proc.Run("dotnet", $"{Creator} -i \"{templateFeedDirectory}\"").WaitForExit();
        }

        public Battery(ITestOutputHelper outputHelper)
        {
            Console.SetOut(new OutputHelperHelper(outputHelper));
            Console.SetError(new OutputHelperHelper(outputHelper));
        }

        [PrettyTheory, MemberData(nameof(Discover))]
        public void Run(TemplateTestData testData)
        {
            bool success = true;
            string[] allParts = new string[testData.Paths.Length + 2];
            allParts[0] = BasePath;
            allParts[1] = "TestCases";

            for (int i = 0; i < testData.Paths.Length; ++i)
            {
                allParts[i + 2] = testData.Paths[i];
            }

            string contents = File.ReadAllText(Path.Combine(allParts));
            contents = Environment.ExpandEnvironmentVariables(contents);

            JObject json = JObject.Parse(contents);

            if (json["skip"]?.Value<bool>() ?? false)
            {
                Console.WriteLine("Test Skipped");
                return;
            }

            string targetPath = Path.Combine(GetTemplatePath(), "_" + Guid.NewGuid().ToString().Replace("-", ""));
            try
            {
                string install = json["install"]?.ToString();
                string command = testData.CreateCommand;

                Console.WriteLine("Testing: " + testData.Name);
                Dictionary<string, string> dict = new Dictionary<string, string>
                {
                    { "targetPath", targetPath},
                    { "targetPathName", Path.GetFileName(targetPath)},
                };

                List<IHandlerResult> results = new List<IHandlerResult>();
                IHandlerResult current;
                string message;

                if (!string.IsNullOrWhiteSpace(install))
                {
                    Console.WriteLine($"Executing step {(results.Count + 1)} (install)...");
                    current = Install(Creator, install);

                    message = current.VerificationSuccess ? $"PASS ({current.Duration})" : $"FAIL ({current.Duration}): {current.FailureMessage}";
                    Console.WriteLine($"    {message}");
                    Console.WriteLine(" ");

                    if (!current.VerificationSuccess)
                    {
                        success = false;
                        Assert.False(true, current.FailureMessage);
                    }
                }

                Console.WriteLine($"Executing step {(results.Count + 1)} (create)...");
                current = Create(Creator, install, testData.CreateCommand, targetPath);

                message = current.VerificationSuccess ? $"PASS ({current.Duration})" : $"FAIL ({current.Duration}): {current.FailureMessage}";
                Console.WriteLine($"    {message}");
                Console.WriteLine(" ");

                if (!current.VerificationSuccess)
                {
                    success = false;
                    Assert.False(true, current.FailureMessage);
                }

                results.Add(current);

                foreach (JObject entry in ((JArray)json["tasks"]).Children().OfType<JObject>())
                {
                    string handlerKey = entry["handler"].ToString();
                    string variationKey = entry["variation"]?.ToString();

                    // running the right variation, or
                    if (string.Equals(testData.Variation, variationKey)
                        || variationKey == null
                        || (testData.Variation == null && variationKey.Equals(string.Empty)))
                    {
                        IHandler handler = HandlerLookup[handlerKey];
                        Console.WriteLine($"Executing step {(results.Count + 1)} ({handler.Summarize(dict, entry)})...");
                        current = handler.Execute(dict, results, entry);
                        message = current.VerificationSuccess ? $"PASS ({current.Duration})" : $"FAIL ({current.Duration}): {current.FailureMessage}";
                        Console.WriteLine($"    {message}");
                        Console.WriteLine(" ");
                        results.Add(current);
                    }
                }

                foreach (IHandlerResult result in results)
                {
                    success = !success ? success : result.VerificationSuccess;
                    Assert.False(!result.VerificationSuccess, result.FailureMessage);
                }
            }
            finally
            {
                if (success)
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        try
                        {
                            DeleteDirectory(targetPath);
                            break;
                        }
                        catch
                        {
                            Thread.Sleep(500);
                        }
                    }
                }
            }
        }

        public static void DeleteDirectory(string directory)
        {
            string[] files = Directory.GetFiles(directory);
            string[] dirs = Directory.GetDirectories(directory);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(directory, true);
        }


        private IHandlerResult Install(string creator, string installPackage)
        {
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                Process install = Proc.Run("dotnet", $"{creator} -i \"{installPackage}\"");
                install.WaitForExit();

                if (install.ExitCode != 0)
                {
                    return new GenericHandlerResult(watch.Elapsed, false, $"\"{installPackage}\" failed to install");
                }

                return new GenericHandlerResult(watch.Elapsed, true, null);
            }
            finally
            {
                watch.Stop();
            }
        }

        private static string FindTemplateFeedDirectory(string batteryDirectory)
        {
            DirectoryInfo currentDirectory = new DirectoryInfo(batteryDirectory);
            string templateFeed = Path.Combine(currentDirectory.FullName, "template_feed");

            while (!Directory.Exists(templateFeed))
            {
                currentDirectory = currentDirectory.Parent;
                templateFeed = Path.Combine(currentDirectory.FullName, "template_feed");
            }

            return templateFeed;
        }

        private static IHandlerResult Create(string creator, string installPackage, string command, string targetPath)
        {
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                Directory.CreateDirectory(targetPath);
                Process create = Proc.Run("dotnet", $"{creator} {command} -o \"{targetPath}\"");
                create.WaitForExit();

                if (create.ExitCode != 0)
                {
                    return new GenericHandlerResult(watch.Elapsed, false, $"\"{command}\" failed create");
                }

                Directory.SetCurrentDirectory(targetPath);
                return new GenericHandlerResult(watch.Elapsed, true, null);
            }
            finally
            {
                watch.Stop();
            }
        }

        private string GetTemplatePath()
        {
            return Path.Combine(BasePath, "TestTemplates");
        }

        public static IEnumerable<object[]> Discover()
        {
            string basePath = Path.Combine(BasePath, "TestCases");

            foreach (string testCase in Directory.EnumerateFiles(basePath, "*.json", SearchOption.AllDirectories))
            {
                string contents = File.ReadAllText(Path.Combine(testCase));
                contents = Environment.ExpandEnvironmentVariables(contents);

                JObject json = JObject.Parse(contents);

                string relPath = testCase.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var data = new TemplateTestData()
                {
                    Paths = relPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    Name = json["name"].ToString(),
                    CreateCommand = json["create"].ToString(),
                    Variation = null
                };

                yield return new object[] { data };

                if (json["variations"] != null)
                {
                    foreach (JObject entry in ((JArray)json["variations"]).Children().OfType<JObject>())
                    {
                        var variation = new TemplateTestData()
                        {
                            Paths = relPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                            Name = entry["name"]?.ToString() ?? data.Name,
                            Variation = entry["id"].ToString(),
                            CreateCommand = entry["create"]?.ToString() ?? data.CreateCommand
                        };
                        yield return new object[] { variation };
                    }
                }
            }
        }
    }
}
