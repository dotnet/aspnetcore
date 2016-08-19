using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.NodeServices.HostingModels
{
    /// <summary>
    /// Class responsible for launching a Node child process on the local machine, determining when it is ready to
    /// accept invocations, detecting if it dies on its own, and finally terminating it on disposal.
    ///
    /// This abstract base class uses the input/output streams of the child process to perform a simple handshake
    /// to determine when the child process is ready to accept invocations. This is agnostic to the mechanism that
    /// derived classes use to actually perform the invocations (e.g., they could use HTTP-RPC, or a binary TCP
    /// protocol, or any other RPC-type mechanism).
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.NodeServices.HostingModels.INodeInstance" />
    public abstract class OutOfProcessNodeInstance : INodeInstance
    {
        protected readonly ILogger OutputLogger;
        private const string ConnectionEstablishedMessage = "[Microsoft.AspNetCore.NodeServices:Listening]";
        private const string DebuggingStartedMessageFormat = @"-----
*** Node.js debugging is enabled ***
{0}

To debug, run:
   node-inspector{1}

If you haven't yet installed node-inspector, you can do so as follows:
   npm install -g node-inspector
-----";
        private readonly TaskCompletionSource<object> _connectionIsReadySource = new TaskCompletionSource<object>();
        private bool _disposed;
        private readonly StringAsTempFile _entryPointScript;
        private FileSystemWatcher _fileSystemWatcher;
        private readonly Process _nodeProcess;
        private int? _nodeDebuggingPort;
        private bool _nodeProcessNeedsRestart;
        private readonly string[] _watchFileExtensions;

        public OutOfProcessNodeInstance(
            string entryPointScript,
            string projectPath,
            string[] watchFileExtensions,
            string commandLineArguments,
            ILogger nodeOutputLogger,
            IDictionary<string, string> environmentVars,
            bool launchWithDebugging,
            int? debuggingPort)
        {
            if (nodeOutputLogger == null)
            {
                throw new ArgumentNullException(nameof(nodeOutputLogger));
            }

            OutputLogger = nodeOutputLogger;
            _entryPointScript = new StringAsTempFile(entryPointScript);

            var startInfo = PrepareNodeProcessStartInfo(_entryPointScript.FileName, projectPath, commandLineArguments,
                environmentVars, launchWithDebugging, debuggingPort);
            _nodeProcess = LaunchNodeProcess(startInfo);
            _watchFileExtensions = watchFileExtensions;
            _fileSystemWatcher = BeginFileWatcher(projectPath);
            ConnectToInputOutputStreams();
        }

        public async Task<T> InvokeExportAsync<T>(string moduleName, string exportNameOrNull, params object[] args)
        {
            if (_nodeProcess.HasExited || _nodeProcessNeedsRestart)
            {
                // This special kind of exception triggers a transparent retry - NodeServicesImpl will launch
                // a new Node instance and pass the invocation to that one instead.
                var message = _nodeProcess.HasExited
                    ? "The Node process has exited"
                    : "The Node process needs to restart";
                throw new NodeInvocationException(message, null, nodeInstanceUnavailable: true);
            }

            // Wait until the connection is established. This will throw if the connection fails to initialize.
            await _connectionIsReadySource.Task;

            return await InvokeExportAsync<T>(new NodeInvocationInfo
            {
                ModuleName = moduleName,
                ExportedFunctionName = exportNameOrNull,
                Args = args
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract Task<T> InvokeExportAsync<T>(NodeInvocationInfo invocationInfo);

        // This method is virtual, as it provides a way to override the NODE_PATH or the path to node.exe
        protected virtual ProcessStartInfo PrepareNodeProcessStartInfo(
            string entryPointFilename, string projectPath, string commandLineArguments,
            IDictionary<string, string> environmentVars, bool launchWithDebugging, int? debuggingPort)
        {
            string debuggingArgs;
            if (launchWithDebugging)
            {
                debuggingArgs = debuggingPort.HasValue ? $"--debug={debuggingPort.Value} " : "--debug ";
                _nodeDebuggingPort = debuggingPort;
            }
            else
            {
                debuggingArgs = string.Empty;    
            }

            var thisProcessPid = Process.GetCurrentProcess().Id;
            var startInfo = new ProcessStartInfo("node")
            {
                Arguments = $"{debuggingArgs}\"{entryPointFilename}\" --parentPid {thisProcessPid} {commandLineArguments ?? string.Empty}",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = projectPath
            };

            // Append environment vars
            if (environmentVars != null)
            {
                foreach (var envVarKey in environmentVars.Keys)
                {
                    var envVarValue = environmentVars[envVarKey];
                    if (envVarValue != null)
                    {
                        SetEnvironmentVariable(startInfo, envVarKey, envVarValue);
                    }
                }
            }

            // Append projectPath to NODE_PATH so it can locate node_modules
            var existingNodePath = Environment.GetEnvironmentVariable("NODE_PATH") ?? string.Empty;
            if (existingNodePath != string.Empty)
            {
                existingNodePath += Path.PathSeparator;
            }

            var nodePathValue = existingNodePath + Path.Combine(projectPath, "node_modules");
            SetEnvironmentVariable(startInfo, "NODE_PATH", nodePathValue);

            return startInfo;
        }

        protected virtual void OnOutputDataReceived(string outputData)
        {
            OutputLogger.LogInformation(outputData);
        }

        protected virtual void OnErrorDataReceived(string errorData)
        {
            OutputLogger.LogError(errorData);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _entryPointScript.Dispose();
                    EnsureFileSystemWatcherIsDisposed();
                }

                // Make sure the Node process is finished
                // TODO: Is there a more graceful way to end it? Or does this still let it perform any cleanup?
                if (!_nodeProcess.HasExited)
                {
                    _nodeProcess.Kill();
                }

                _disposed = true;
            }
        }

        private void EnsureFileSystemWatcherIsDisposed()
        {
            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.Dispose();
                _fileSystemWatcher = null;
            }
        }

        private static void SetEnvironmentVariable(ProcessStartInfo startInfo, string name, string value)
        {
#if NET451
            startInfo.EnvironmentVariables[name] = value;
#else
            startInfo.Environment[name] = value;
#endif
        }

        private static Process LaunchNodeProcess(ProcessStartInfo startInfo)
        {
            var process = Process.Start(startInfo);

            // On Mac at least, a killed child process is left open as a zombie until the parent
            // captures its exit code. We don't need the exit code for this process, and don't want
            // to use process.WaitForExit() explicitly (we'd have to block the thread until it really
            // has exited), but we don't want to leave zombies lying around either. It's sufficient
            // to use process.EnableRaisingEvents so that .NET will grab the exit code and let the
            // zombie be cleaned away without having to block our thread.
            process.EnableRaisingEvents = true;

            return process;
        }

        private static string UnencodeNewlines(string str)
        {
            if (str != null)
            {
                // The token here needs to match the const in OverrideStdOutputs.ts.
                // See the comment there for why we're doing this.
                str = str.Replace("__ns_newline__", Environment.NewLine);
            }

            return str;
        }

        private void ConnectToInputOutputStreams()
        {
            var initializationIsCompleted = false;

            _nodeProcess.OutputDataReceived += (sender, evt) =>
            {
                if (evt.Data == ConnectionEstablishedMessage && !initializationIsCompleted)
                {
                    _connectionIsReadySource.SetResult(null);
                    initializationIsCompleted = true;
                }
                else if (evt.Data != null)
                {
                    OnOutputDataReceived(UnencodeNewlines(evt.Data));
                }
            };

            _nodeProcess.ErrorDataReceived += (sender, evt) =>
            {
                if (evt.Data != null)
                {
                    if (IsDebuggerListeningMessage(evt.Data))
                    {
                        var debugPortArg = _nodeDebuggingPort.HasValue ? $" --debug-port={_nodeDebuggingPort.Value}" : string.Empty;
                        OutputLogger.LogWarning(string.Format(DebuggingStartedMessageFormat, evt.Data, debugPortArg));
                    }
                    else if (!initializationIsCompleted)
                    {
                        _connectionIsReadySource.SetException(
                            new InvalidOperationException("The Node.js process failed to initialize: " + evt.Data));
                        initializationIsCompleted = true;
                    }
                    else
                    {
                        OnErrorDataReceived(UnencodeNewlines(evt.Data));
                    }
                }
            };

            _nodeProcess.BeginOutputReadLine();
            _nodeProcess.BeginErrorReadLine();
        }

        private static bool IsDebuggerListeningMessage(string message)
        {
            return message.StartsWith("Debugger listening ", StringComparison.OrdinalIgnoreCase);
        }

        private FileSystemWatcher BeginFileWatcher(string rootDir)
        {
            if (_watchFileExtensions == null || _watchFileExtensions.Length == 0)
            {
                // Nothing to watch
                return null;
            }

            var watcher = new FileSystemWatcher(rootDir)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Deleted += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            watcher.EnableRaisingEvents = true;
            return watcher;
        }

        private void OnFileChanged(object source, FileSystemEventArgs e)
        {
            if (IsFilenameBeingWatched(e.FullPath))
            {
                RestartDueToFileChange(e.FullPath);
            }
        }

        private void OnFileRenamed(object source, RenamedEventArgs e)
        {
            if (IsFilenameBeingWatched(e.OldFullPath) || IsFilenameBeingWatched(e.FullPath))
            {
                RestartDueToFileChange(e.OldFullPath);
            }
        }

        private bool IsFilenameBeingWatched(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return false;
            }
            else
            {
                var actualExtension = Path.GetExtension(fullPath) ?? string.Empty;
                return _watchFileExtensions.Any(actualExtension.Equals);
            }
        }

        private void RestartDueToFileChange(string fullPath)
        {
            OutputLogger.LogInformation($"Node will restart because file changed: {fullPath}");

            _nodeProcessNeedsRestart = true;

            // There's no need to watch for any more changes, since we're already restarting, and if the
            // restart takes some time (e.g., due to connection draining), we could end up getting duplicate
            // notifications.
            EnsureFileSystemWatcherIsDisposed();
        }

        ~OutOfProcessNodeInstance()
        {
            Dispose(false);
        }
    }
}