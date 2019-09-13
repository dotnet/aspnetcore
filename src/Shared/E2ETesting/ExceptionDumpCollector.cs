// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.E2ETesting
{
    public class ExceptionDumpCollector : IDisposable
    {
        private static IMessageSink _diagnosticsMessageSink;
        private readonly Process _procDumpProcess;

        public ExceptionDumpCollector(IMessageSink diagnosticsMessageSink)
        {
            _diagnosticsMessageSink = diagnosticsMessageSink;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _diagnosticsMessageSink.OnMessage(new DiagnosticMessage("Running on non Windows platform. Skipping dump capture."));
                return;
            }

            if (E2ETestOptions.Instance.CaptureProcessDumpOnAssertionFailure)
            {
                var procDumpPath = E2ETestOptions.Instance.ProcDumpPath;
                if (string.IsNullOrWhiteSpace(procDumpPath))
                {
                    _diagnosticsMessageSink.OnMessage(new DiagnosticMessage("ProcDumpPath not specified."));
                    return;
                }

                if (!Directory.Exists(procDumpPath))
                {
                    _diagnosticsMessageSink.OnMessage(new DiagnosticMessage($";{procDumpPath}; doesn't exist."));
                    return;
                }

                var dumpsFolder = E2ETestOptions.Instance.ProcessDumpsFolder;
                if (string.IsNullOrWhiteSpace(dumpsFolder))
                {
                    _diagnosticsMessageSink.OnMessage(new DiagnosticMessage("ProcessDumpsFolder not specified."));
                    return;
                }

                if (!Directory.Exists(dumpsFolder))
                {
                    _diagnosticsMessageSink.OnMessage(new DiagnosticMessage($";{dumpsFolder}; doesn't exist."));
                    return;
                }

                var procDumpFullPath = Path.Combine(procDumpPath, "procdump.exe");
                if (!File.Exists(procDumpFullPath))
                {
                    _diagnosticsMessageSink.OnMessage(new DiagnosticMessage($"'{procDumpFullPath}' doesn't exist."));
                    return;
                }

                var currentProcess = Process.GetCurrentProcess();
                _procDumpProcess = StartProcDump(currentProcess, procDumpFullPath, dumpsFolder);
                Thread.Sleep(1000);
                if (_procDumpProcess.HasExited)
                {
                    var stdOutput = _procDumpProcess.StandardOutput.ReadToEnd();
                    var errorOutput = _procDumpProcess.StandardError.ReadToEnd();

                    _diagnosticsMessageSink.OnMessage(new DiagnosticMessage($"'ProcDump exited with code '{_procDumpProcess.ExitCode}'"));
                    _diagnosticsMessageSink.OnMessage(new DiagnosticMessage(stdOutput));
                    _diagnosticsMessageSink.OnMessage(new DiagnosticMessage(errorOutput));
                }
                else
                {
                    _diagnosticsMessageSink.OnMessage(new DiagnosticMessage("Running tests with ProcDump watching for first chance exceptions."));
                }
            }
            else
            {
                _diagnosticsMessageSink.OnMessage(new DiagnosticMessage("Capturing process dumps on assertion failures is disabled."));
            }
        }

        private Process StartProcDump(
            Process currentProcess,
            string procDumpFullPath,
            string dumpsFolder)
        {
            var procDumpPattern = Path.Combine(dumpsFolder, "TestFailure_PROCESSNAME_PID_YYMMDD_HHMMSS.dmp");
            var processStartInfo = new ProcessStartInfo(
                procDumpFullPath,
                $"-accepteula -ma -e 1 -f \"CLR\" {currentProcess.Id} {procDumpPattern}")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            return Process.Start(processStartInfo);
        }

        public void Dispose()
        {
            if (_procDumpProcess != null)
            {
                if (!_procDumpProcess.HasExited)
                {
                    _diagnosticsMessageSink.OnMessage(new DiagnosticMessage("ProcDump has run successfully until test session end."));
                }
                else
                {
                    _diagnosticsMessageSink.OnMessage(new DiagnosticMessage($"ProcDump has failed with error '{_procDumpProcess.ExitCode}' during the test session."));
                }

                // We don't need to kill ProcDump as it will terminate automatically when this process exists.
                _procDumpProcess.Dispose();
            }
        }
    }
}
