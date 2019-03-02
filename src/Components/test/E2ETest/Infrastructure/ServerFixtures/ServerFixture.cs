// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures
{
    public abstract class ServerFixture : IDisposable
    {
        private static readonly Lazy<Dictionary<string, string>> _projects = new Lazy<Dictionary<string, string>>(FindProjects);

        public Uri RootUri => _rootUriInitializer.Value;

        private readonly Lazy<Uri> _rootUriInitializer;

        public ServerFixture()
        {
            _rootUriInitializer = new Lazy<Uri>(() =>
                new Uri(StartAndGetRootUri()));
        }

        public abstract void Dispose();

        protected abstract string StartAndGetRootUri();

        protected static string FindSolutionDir()
        {
            return FindClosestDirectoryContaining(
                "Components.sln",
                Path.GetDirectoryName(typeof(ServerFixture).Assembly.Location));
        }

        private static Dictionary<string, string> FindProjects()
        {
            var solutionDir = FindSolutionDir();

            var testAssetsDirectories = new[]
            {
                Path.Combine(solutionDir, "test", "testassets"),
                Path.Combine(solutionDir, "blazor", "testassets"),
            };

            return testAssetsDirectories
                .SelectMany(d => new DirectoryInfo(d).EnumerateDirectories())
                .ToDictionary(d => d.Name, d => d.FullName);
        }

        protected static string FindSampleOrTestSitePath(string projectName)
        {
            var projects = _projects.Value;
            if (projects.TryGetValue(projectName, out var dir))
            {
                return dir;
            }

            throw new ArgumentException($"Cannot find a sample or test site with name '{projectName}'.");
        }

        private static string FindClosestDirectoryContaining(
            string filename,
            string startDirectory)
        {
            var dir = startDirectory;
            while (true)
            {
                if (File.Exists(Path.Combine(dir, filename)))
                {
                    return dir;
                }

                dir = Directory.GetParent(dir)?.FullName;
                if (string.IsNullOrEmpty(dir))
                {
                    throw new FileNotFoundException(
                        $"Could not locate a file called '{filename}' in " +
                        $"directory '{startDirectory}' or any parent directory.");
                }
            }
        }

        protected static void RunInBackgroundThread(Action action)
        {
            var isDone = new ManualResetEvent(false);

            new Thread(() =>
            {
                action();
                isDone.Set();
            }).Start();

            isDone.WaitOne();
        }
    }
}
