// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.DotNet.Archive
{
    public class ConsoleProgressReport : IProgress<ProgressReport>
    {
        private string _currentPhase;
        private int _lastLineLength = 0;
        private double _lastProgress = -1;
        private Stopwatch _stopwatch;
        private object _stateLock = new object();

        public void Report(ProgressReport value)
        {
            long progress = (long)(100 * ((double)value.Ticks / value.Total));

            if (progress == _lastProgress && value.Phase == _currentPhase)
            {
                return;
            }
            _lastProgress = progress;

            lock (_stateLock)
            {
                string line = $"{value.Phase} {progress}%";
                if (value.Phase == _currentPhase)
                {
                    if (Console.IsOutputRedirected)
                    {
                        Console.Write($"...{progress}%");
                    }
                    else
                    {
                        Console.Write(new string('\b', _lastLineLength));
                        Console.Write(line);
                    }

                    _lastLineLength = line.Length;

                    if (progress == 100)
                    {
                        Console.WriteLine($" {_stopwatch.ElapsedMilliseconds} ms");
                    }
                }
                else
                {
                    Console.Write(line);
                    _currentPhase = value.Phase;
                    _lastLineLength = line.Length;
                    _stopwatch = Stopwatch.StartNew();
                }
            }
        }
    }
}
