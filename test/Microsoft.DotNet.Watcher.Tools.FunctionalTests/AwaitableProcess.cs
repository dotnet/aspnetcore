// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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
        private BufferBlock<string> _source;
        private ITestOutputHelper _logger;
        private int _reading;

        public AwaitableProcess(ProcessSpec spec, ITestOutputHelper logger)
        {
            _spec = spec;
            _logger = logger;
        }

        public void Start()
        {
            if (_process != null)
            {
                throw new InvalidOperationException("Already started");
            }

            var psi = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = _spec.Executable,
                WorkingDirectory = _spec.WorkingDirectory,
                Arguments = ArgumentEscaper.EscapeAndConcatenate(_spec.Arguments),
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            _process = Process.Start(psi);
            _logger.WriteLine($"{DateTime.Now}: process start: '{psi.FileName} {psi.Arguments}'");
            StartProcessingOutput(_process.StandardOutput);
            StartProcessingOutput(_process.StandardError);;
        }

        public Task<string> GetOutputLineAsync(string message)
            => GetOutputLineAsync(m => message == m);

        public async Task<string> GetOutputLineAsync(Predicate<string> predicate)
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

        private void StartProcessingOutput(StreamReader streamReader)
        {
            _source = _source ?? new BufferBlock<string>();
            Interlocked.Increment(ref _reading);
            Task.Run(() =>
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    _logger.WriteLine($"{DateTime.Now}: post: '{line}'");
                    _source.Post(line);
                }

                if (Interlocked.Decrement(ref _reading) <= 0)
                {
                    _source.Complete();
                }
            }).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_process != null && !_process.HasExited)
            {
                _process.KillTree();
            }
        }
    }
}