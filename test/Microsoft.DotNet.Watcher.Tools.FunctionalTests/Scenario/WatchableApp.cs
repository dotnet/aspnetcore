// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.Utils;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class WatchableApp : IDisposable
    {
        private const string StartedMessage = "Started";
        private const string ExitingMessage = "Exiting";

        protected ProjectToolScenario Scenario { get; }
        private readonly ITestOutputHelper _logger;
        protected AwaitableProcess Process { get; set; }
        private string _appName;
        private bool _prepared;

        public WatchableApp(string appName, ITestOutputHelper logger)
        {
            _logger = logger;
            _appName = appName;
            Scenario = new ProjectToolScenario(logger);
            Scenario.AddTestProjectFolder(appName);
            SourceDirectory = Path.Combine(Scenario.WorkFolder, appName);
        }

        public string SourceDirectory { get; }

        public Task HasRestarted()
            => Process.GetOutputLineAsync(StartedMessage);

        public Task HasExited()
            => Process.GetOutputLineAsync(ExitingMessage);

        public bool UsePollingWatcher { get; set; }

        public Task StartWatcher([CallerMemberName] string name = null)
            => StartWatcher(Array.Empty<string>(), name);

        public async Task<int> GetProcessId()
        {
            var line = await Process.GetOutputLineAsync(l => l.StartsWith("PID ="));
            var pid = line.Split('=').Last();
            return int.Parse(pid);
        }

        public void Prepare()
        {
            Scenario.Restore(_appName);
            Scenario.Build(_appName);
            _prepared = true;
        }

        public async Task StartWatcher(string[] arguments, [CallerMemberName] string name = null)
        {
            if (!_prepared)
            {
                throw new InvalidOperationException("Call .Prepare() first");
            }

            var args = Scenario
                .GetDotnetWatchArguments()
                .Concat(new[] { "run", "--" })
                .Concat(arguments);

            var spec = new ProcessSpec
            {
                Executable = new Muxer().MuxerPath,
                Arguments = args,
                WorkingDirectory = SourceDirectory
            };

            Process = new AwaitableProcess(spec, _logger);
            Process.Start();
            await Process.GetOutputLineAsync(StartedMessage);
        }

        public virtual void Dispose()
        {
            Process.Dispose();
            Scenario.Dispose();
        }
    }
}
