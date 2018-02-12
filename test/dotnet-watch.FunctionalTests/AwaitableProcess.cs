// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.CommandLineUtils;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class AwaitableProcess : IDisposable
    {
        private Process _process;
        private readonly ProcessSpec _spec;
        private BufferBlock<string> _source;
        private ITestOutputHelper _logger;

        public AwaitableProcess(ProcessSpec spec, ITestOutputHelper logger)
        {
            _spec = spec;
            _logger = logger;
            _source = new BufferBlock<string>();
        }

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

            _process.OutputDataReceived += OnData;
            _process.ErrorDataReceived += OnData;
            _process.Exited += OnExit;

            _process.Start();
            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();
            _logger.WriteLine($"{DateTime.Now}: process start: '{_process.StartInfo.FileName} {_process.StartInfo.Arguments}'");
        }

        public async Task<string> GetOutputLineAsync(string message, TimeSpan timeout)
        {
            _logger.WriteLine($"Waiting for output line [msg == '{message}']. Will wait for {timeout.TotalSeconds} sec.");
            return await GetOutputLineAsync(m => message == m).TimeoutAfter(timeout);
        }

        public async Task<string> GetOutputLineStartsWithAsync(string message, TimeSpan timeout)
        {
            _logger.WriteLine($"Waiting for output line [msg.StartsWith('{message}')]. Will wait for {timeout.TotalSeconds} sec.");
            return await GetOutputLineAsync(m => m.StartsWith(message)).TimeoutAfter(timeout);
        }

        private async Task<string> GetOutputLineAsync(Predicate<string> predicate)
        {
            while (!_source.Completion.IsCompleted)
            {
                while (await _source.OutputAvailableAsync())
                {
                    var next = await _source.ReceiveAsync();
                    _logger.WriteLine($"{DateTime.Now}: recv: '{next}'");
                    if (predicate(next))
                    {
                        return next;
                    }
                }
            }

            return null;
        }

        public async Task<IList<string>> GetAllOutputLines()
        {
            var lines = new List<string>();
            while (!_source.Completion.IsCompleted)
            {
                while (await _source.OutputAvailableAsync())
                {
                    var next = await _source.ReceiveAsync();
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
            _source.Complete();
        }

        public void Dispose()
        {
            _source.Complete();

            if (_process != null)
            {
                if (!_process.HasExited)
                {
                    _process.KillTree();
                }

                _process.ErrorDataReceived -= OnData;
                _process.OutputDataReceived -= OnData;
                _process.Exited -= OnExit;
            }
        }
    }
}
