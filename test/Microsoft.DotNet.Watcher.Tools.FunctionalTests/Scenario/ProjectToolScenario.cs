// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.DependencyModel;
using Microsoft.DotNet.ProjectModel;
using System.Reflection;
using System.Linq;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class ProjectToolScenario : IDisposable
    {
        private const string NugetConfigFileName = "NuGet.config";
        private static readonly string TestProjectSourceRoot = Path.Combine(AppContext.BaseDirectory, "TestProjects");

        private static readonly object _restoreLock = new object();

        public ProjectToolScenario()
        {
            Console.WriteLine($"The temporary test folder is {TempFolder}");

            WorkFolder = Path.Combine(TempFolder, "work");

            CreateTestDirectory();
        }

        public string TempFolder { get; } = Path.Combine(Path.GetDirectoryName(FindNugetConfig()), "testWorkDir", Guid.NewGuid().ToString("N"));

        public string WorkFolder { get; }

        public void AddTestProjectFolder(string projectName)
        {
            var srcFolder = Path.Combine(TestProjectSourceRoot, projectName);
            var destinationFolder = Path.Combine(WorkFolder, Path.GetFileName(projectName));
            Console.WriteLine($"Copying project {srcFolder} to {destinationFolder}");

            Directory.CreateDirectory(destinationFolder);

            foreach (var directory in Directory.GetDirectories(srcFolder, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(directory.Replace(srcFolder, destinationFolder));
            }

            foreach (var file in Directory.GetFiles(srcFolder, "*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(srcFolder, destinationFolder), true);
            }
        }

        public void Restore(string project = null)
        {
            if (project == null)
            {
                project = WorkFolder;
            }
            else
            {
                project = Path.Combine(WorkFolder, project);
            }

            // Tests are run in parallel and they try to restore tools concurrently.
            // This causes issues because the deps json file for a tool is being written from
            // multiple threads - which results in either sharing violation or corrupted json.
            lock (_restoreLock)
            {
                var restore = Command
                    .CreateDotNet("restore", new[] { project })
                    .Execute();

                if (restore.ExitCode != 0)
                {
                    throw new Exception($"Exit code {restore.ExitCode}");
                }
            }
        }

        private void CreateTestDirectory()
        {
            Directory.CreateDirectory(WorkFolder);
            File.WriteAllText(Path.Combine(WorkFolder, "global.json"), "{}");

            var nugetConfigFilePath = FindNugetConfig();

            var tempNugetConfigFile = Path.Combine(WorkFolder, NugetConfigFileName);
            File.Copy(nugetConfigFilePath, tempNugetConfigFile);
        }

        public Process ExecuteDotnetWatch(IEnumerable<string> arguments, string workDir, IDictionary<string, string> environmentVariables = null)
        {
            // this launches a new .NET Core process using the runtime of the current test app 
            // and the version of dotnet-watch that this test app is compiled against
            var thisAssembly = Path.GetFileNameWithoutExtension(GetType().GetTypeInfo().Assembly.Location);
            var args = new List<string>();
            args.Add("exec");

            args.Add("--depsfile");
            args.Add(Path.Combine(AppContext.BaseDirectory, thisAssembly + FileNameSuffixes.DepsJson));

            args.Add("--runtimeconfig");
            args.Add(Path.Combine(AppContext.BaseDirectory, thisAssembly + FileNameSuffixes.RuntimeConfigJson));

            args.Add(Path.Combine(AppContext.BaseDirectory, "dotnet-watch.dll"));

            var argsStr = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args.Concat(arguments));

            Console.WriteLine($"Running dotnet {argsStr} in {workDir}");

            var psi = new ProcessStartInfo(new Muxer().MuxerPath, argsStr)
            {
                UseShellExecute = false,
                WorkingDirectory = workDir,
            };

            if (environmentVariables != null)
            {
                foreach (var newEnvVar in environmentVariables)
                {
                    var varKey = newEnvVar.Key;
                    var varValue = newEnvVar.Value;
#if NET451
                    psi.EnvironmentVariables[varKey] = varValue;

#else
                    psi.Environment[varKey] = varValue;
#endif
                }
            }

            return Process.Start(psi);
        }

        private static string FindNugetConfig()
        {
            var currentDirPath = Directory.GetCurrentDirectory();

            string nugetConfigFile;
            while (true)
            {
                nugetConfigFile = Path.Combine(currentDirPath, NugetConfigFileName);
                if (File.Exists(nugetConfigFile))
                {
                    break;
                }

                currentDirPath = Path.GetDirectoryName(currentDirPath);
            }

            return nugetConfigFile;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(TempFolder, recursive: true);
            }
            catch
            {
                Console.WriteLine($"Failed to delete {TempFolder}. Retrying...");
                Thread.Sleep(TimeSpan.FromSeconds(5));
                Directory.Delete(TempFolder, recursive: true);
            }
        }
    }
}