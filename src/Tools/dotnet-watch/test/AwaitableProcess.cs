// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.CommandLineUtils;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class AwaitableProcess : IDisposable
    {
        private Process _process;
        private readonly ProcessSpec _spec;
        private readonly List<string> _lines;
        private BufferBlock<string> _source;
        private ITestOutputHelper _logger;
        private TaskCompletionSource<int> _exited;
        private bool _started;

        public AwaitableProcess(ProcessSpec spec, ITestOutputHelper logger)
        {
            _spec = spec;
            _logger = logger;
            _source = new BufferBlock<string>();
            _lines = new List<string>();
            _exited = new TaskCompletionSource<int>();
        }

        public IEnumerable<string> Output => _lines;

        public Task Exited => _exited.Task;

        public int Id => _process.Id;

        public void Start()
        {
            if (_process != null)
            {
                throw new InvalidOperationException("Already started");
            }

            _process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    FileName = _spec.Executable,
                    WorkingDirectory = _spec.WorkingDirectory,
                    Arguments = ArgumentEscaper.EscapeAndConcatenate(_spec.Arguments),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Environment =
                    {
                        ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "true"
                    }
                }
            };

            foreach (var env in _spec.EnvironmentVariables)
            {
                _process.StartInfo.EnvironmentVariables[env.Key] = env.Value;
            }

            _process.OutputDataReceived += OnData;
            _process.ErrorDataReceived += OnData;
            _process.Exited += OnExit;

            _logger.WriteLine($"{DateTime.Now}: starting process: '{_process.StartInfo.FileName} {_process.StartInfo.Arguments}'");
            _process.Start();
            _started = true;
            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();
            _logger.WriteLine($"{DateTime.Now}: process started: '{_process.StartInfo.FileName} {_process.StartInfo.Arguments}'");
        }

        public async Task<string> GetOutputLineAsync(string message, TimeSpan timeout)
        {
            _logger.WriteLine($"Waiting for output line [msg == '{message}']. Will wait for {timeout.TotalSeconds} sec.");
            var cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);
            return await GetOutputLineAsync($"[msg == '{message}']", m => string.Equals(m, message, StringComparison.Ordinal), cts.Token);
        }

        public async Task<string> GetOutputLineStartsWithAsync(string message, TimeSpan timeout)
        {
            _logger.WriteLine($"Waiting for output line [msg.StartsWith('{message}')]. Will wait for {timeout.TotalSeconds} sec.");
            var cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);
            return await GetOutputLineAsync($"[msg.StartsWith('{message}')]", m => m != null && m.StartsWith(message, StringComparison.Ordinal), cts.Token);
        }

        private async Task<string> GetOutputLineAsync(string predicateName, Predicate<string> predicate, CancellationToken cancellationToken)
        {
            while (!_source.Completion.IsCompleted)
            {
                while (await _source.OutputAvailableAsync(cancellationToken))
                {
                    var next = await _source.ReceiveAsync(cancellationToken);
                    _lines.Add(next);
                    var match = predicate(next);
                    _logger.WriteLine($"{DateTime.Now}: recv: '{next}'. {(match ? "Matches" : "Does not match")} condition '{predicateName}'.");
                    if (match)
                    {
                        return next;
                    }
                }
            }

            return null;
        }

        public async Task<IList<string>> GetAllOutputLinesAsync(CancellationToken cancellationToken)
        {
            var lines = new List<string>();
            while (!_source.Completion.IsCompleted)
            {
                while (await _source.OutputAvailableAsync(cancellationToken))
                {
                    var next = await _source.ReceiveAsync(cancellationToken);
                    _logger.WriteLine($"{DateTime.Now}: recv: '{next}'");
                    lines.Add(next);
                }
            }
            return lines;
        }

        private void OnData(object sender, DataReceivedEventArgs args)
        {
            var line = args.Data ?? string.Empty;
            _logger.WriteLine($"{DateTime.Now}: post: '{line}'");
            _source.Post(line);
        }

        private void OnExit(object sender, EventArgs args)
        {
            // Wait to ensure the process has exited and all output consumed
            _process.WaitForExit();
            _source.Complete();
            _exited.TrySetResult(_process.ExitCode);
            _logger.WriteLine($"Process {_process.Id} has exited");
        }

        public void Dispose()
        {
            _source.Complete();

            if (_process != null)
            {
                if (_started && !_process.HasExited)
                {
                    _process.KillTree();
                }

                _process.ErrorDataReceived -= OnData;
                _process.OutputDataReceived -= OnData;
                _process.Exited -= OnExit;
                _process.Dispose();
            }
        }
    }
}
