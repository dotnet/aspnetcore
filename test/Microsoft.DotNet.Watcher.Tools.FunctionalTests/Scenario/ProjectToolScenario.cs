// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class ProjectToolScenario : IDisposable
    {
        private const string NugetConfigFileName = "NuGet.config";

        private static readonly object _restoreLock = new object();

        public ProjectToolScenario()
        {
            Console.WriteLine($"The temporary test folder is {TempFolder}");

            WorkFolder = Path.Combine(TempFolder, "work");

            CreateTestDirectory();
        }

        public string TempFolder { get; } = Path.Combine(Path.GetDirectoryName(FindNugetConfig()), "testWorkDir", Guid.NewGuid().ToString());

        public string WorkFolder { get; }

        public void AddProject(string projectFolder)
        {
            var destinationFolder = Path.Combine(WorkFolder, Path.GetFileName(projectFolder));
            Console.WriteLine($"Copying project {projectFolder} to {destinationFolder}");

            Directory.CreateDirectory(destinationFolder);

            foreach (var directory in Directory.GetDirectories(projectFolder, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(directory.Replace(projectFolder, destinationFolder));
            }

            foreach (var file in Directory.GetFiles(projectFolder, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(file, file.Replace(projectFolder, destinationFolder), true);
            }
        }

        public void AddNugetFeed(string feedName, string feed)
        {
            var tempNugetConfigFile = Path.Combine(WorkFolder, NugetConfigFileName);

            var nugetConfig = XDocument.Load(tempNugetConfigFile);
            var packageSource = nugetConfig.Element("configuration").Element("packageSources");
            packageSource.Add(new XElement("add", new XAttribute("key", feedName), new XAttribute("value", feed)));
            using (var stream = File.OpenWrite(tempNugetConfigFile))
            {
                nugetConfig.Save(stream);
            }
        }

        public void AddToolToProject(string projectName, string toolName)
        {
            var projectFile = Path.Combine(WorkFolder, projectName, "project.json");
            Console.WriteLine($"Adding {toolName} to {projectFile}");

            var projectJson = JObject.Parse(File.ReadAllText(projectFile));
            projectJson.Add("tools",
              new JObject(
                new JProperty(toolName,
                  new JObject(
                    new JProperty("version", "1.0.0-*"),
                    new JProperty("target", "package")))));

            File.WriteAllText(projectFile, projectJson.ToString());
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
                var restore = ExecuteDotnet($"restore -v Minimal", project);
                restore.WaitForExit();

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

        public Process ExecuteDotnet(string arguments, string workDir, IDictionary<string, string> environmentVariables = null)
        {
            Console.WriteLine($"Running dotnet {arguments} in {workDir}");

            var psi = new ProcessStartInfo("dotnet", arguments)
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