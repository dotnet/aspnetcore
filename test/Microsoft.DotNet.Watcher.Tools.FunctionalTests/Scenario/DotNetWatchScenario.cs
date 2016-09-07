// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Internal;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class DotNetWatchScenario : IDisposable
    {
        protected const string DotnetWatch = "Microsoft.DotNet.Watcher.Tools";

        protected static readonly string _repositoryRoot = FindRepoRoot();
        protected static readonly string _artifactsFolder = Path.Combine(_repositoryRoot, "artifacts", "build");

        protected ProjectToolScenario _scenario;

        public DotNetWatchScenario()
        {
            _scenario = new ProjectToolScenario();
            Directory.CreateDirectory(_artifactsFolder);
            _scenario.AddNugetFeed(DotnetWatch, _artifactsFolder);
        }

        public Process WatcherProcess { get; private set; }

        public bool UsePollingWatcher { get; set; }

        protected void RunDotNetWatch(string arguments, string workingFolder)
        {
            IDictionary<string, string> envVariables = null;
            if (UsePollingWatcher)
            {
                envVariables = new Dictionary<string, string>()
                {
                    ["DOTNET_USE_POLLING_FILE_WATCHER"] = "true"
                };
            }

            WatcherProcess = _scenario.ExecuteDotnet("watch " + arguments, workingFolder, envVariables);
        }

        public virtual void Dispose()
        {
            if (WatcherProcess != null)
            {
                if (!WatcherProcess.HasExited)
                {
                    WatcherProcess.KillTree();
                }
                WatcherProcess.Dispose();
            }
            _scenario.Dispose();
        }

        private static string FindRepoRoot()
        {
            var di = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (di.Parent != null)
            {
                var globalJsonFile = Path.Combine(di.FullName, "global.json");

                if (File.Exists(globalJsonFile))
                {
                    return di.FullName;
                }

                di = di.Parent;
            }

            return null;
        }
    }
}
