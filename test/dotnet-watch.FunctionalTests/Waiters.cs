// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Microsoft.DotNet.Watcher.FunctionalTests
{
    public static class Waiters
    {
        public static void WaitForFileToBeReadable(string file, TimeSpan timeout)
        {
            var watch = new Stopwatch();

            Exception lastException = null;

            watch.Start();
            while (watch.Elapsed < timeout)
            {
                try
                {
                    File.ReadAllText(file);
                    watch.Stop();
                    return;
                }
                catch (Exception e)
                {
                    lastException = e;
                }
                Thread.Sleep(500);
            }
            watch.Stop();

            if (lastException != null)
            {
                Console.WriteLine("Last exception:");
                Console.WriteLine(lastException);
            }

            throw new InvalidOperationException($"{file} is not readable.");
        }

        public static void WaitForProcessToStop(int processId, TimeSpan timeout, bool expectedToStop, string errorMessage)
        {
            Process process = null;

            try
            {
                process = Process.GetProcessById(processId);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not get process id:");
                Console.WriteLine(e);
            }

            var watch = new Stopwatch();
            watch.Start();
            while (watch.Elapsed < timeout)
            {
                if (process == null || process.HasExited)
                {
                    break;
                }
                Thread.Sleep(500);
            }
            watch.Stop();

            bool isStopped = process == null || process.HasExited;
            if (isStopped != expectedToStop)
            {
                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}
