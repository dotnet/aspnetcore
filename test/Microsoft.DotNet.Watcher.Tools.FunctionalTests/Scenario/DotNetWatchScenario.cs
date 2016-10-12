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
        protected ProjectToolScenario _scenario;

        public DotNetWatchScenario()
        {
            _scenario = new ProjectToolScenario();
        }

        public Process WatcherProcess { get; private set; }

        public bool UsePollingWatcher { get; set; }

        protected void RunDotNetWatch(IEnumerable<string> arguments, string workingFolder)
        {
            IDictionary<string, string> envVariables = null;
            if (UsePollingWatcher)
            {
                envVariables = new Dictionary<string, string>()
                {
                    ["DOTNET_USE_POLLING_FILE_WATCHER"] = "true"
                };
            }

            WatcherProcess = _scenario.ExecuteDotnetWatch(arguments, workingFolder, envVariables);
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
    }
}
