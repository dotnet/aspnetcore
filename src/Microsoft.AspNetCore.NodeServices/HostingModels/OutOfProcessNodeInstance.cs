using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.NodeServices
{
    /// <summary>
    /// Class responsible for launching the Node child process, determining when it is ready to accept invocations,
    /// and finally killing it when the parent process exits. Also it restarts the child process if it dies.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.NodeServices.INodeServices" />
    public abstract class OutOfProcessNodeInstance : INodeServices
    {
        private readonly object _childProcessLauncherLock;
        private string _commandLineArguments;
        private readonly StringAsTempFile _entryPointScript;
        private Process _nodeProcess;
        private TaskCompletionSource<bool> _nodeProcessIsReadySource;
        private readonly string _projectPath;
        private bool _disposed;

        public OutOfProcessNodeInstance(string entryPointScript, string projectPath, string commandLineArguments = null)
        {
            _childProcessLauncherLock = new object();
            _entryPointScript = new StringAsTempFile(entryPointScript);
            _projectPath = projectPath;
            _commandLineArguments = commandLineArguments ?? string.Empty;
        }
        
        public string CommandLineArguments
        {
            get { return _commandLineArguments; }
            set { _commandLineArguments = value; }
        }

        protected Process NodeProcess
        {
            get
            {
                // This is only exposed to support the unreliable InputOutputStreamNodeInstance, which is just to verify that
                // other hosting/transport mechanisms are possible. This shouldn't really be exposed, and will be removed.
                return this._nodeProcess;
            }
        }

        public Task<T> Invoke<T>(string moduleName, params object[] args)
            => InvokeExport<T>(moduleName, null, args);

        public Task<T> InvokeExport<T>(string moduleName, string exportedFunctionName, params object[] args)
        {
            return Invoke<T>(new NodeInvocationInfo
            {
                ModuleName = moduleName,
                ExportedFunctionName = exportedFunctionName,
                Args = args
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public abstract Task<T> Invoke<T>(NodeInvocationInfo invocationInfo);

        protected void ExitNodeProcess()
        {
            if (_nodeProcess != null && !_nodeProcess.HasExited)
            {
                // TODO: Is there a more graceful way to end it? Or does this still let it perform any cleanup?
                _nodeProcess.Kill();
            }
        }

        protected async Task EnsureReady()
        {
            lock (_childProcessLauncherLock)
            {
                if (_nodeProcess == null || _nodeProcess.HasExited)
                {
                    this.OnBeforeLaunchProcess();

                    var startInfo = new ProcessStartInfo("node")
                    {
                        Arguments = "\"" + _entryPointScript.FileName + "\" " + _commandLineArguments,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = _projectPath
                    };

                    // Append projectPath to NODE_PATH so it can locate node_modules
                    var existingNodePath = Environment.GetEnvironmentVariable("NODE_PATH") ?? string.Empty;
                    if (existingNodePath != string.Empty)
                    {
                        existingNodePath += ":";
                    }

                    var nodePathValue = existingNodePath + Path.Combine(_projectPath, "node_modules");
#if NET451
                    startInfo.EnvironmentVariables.Add("NODE_PATH", nodePathValue);
#else
                    startInfo.Environment.Add("NODE_PATH", nodePathValue);
#endif

                    _nodeProcess = Process.Start(startInfo);
                    ConnectToInputOutputStreams();
                }
            }

            var task = _nodeProcessIsReadySource.Task;
            var initializationSucceeded = await task;

            if (!initializationSucceeded)
            {
                throw new InvalidOperationException("The Node.js process failed to initialize", task.Exception);
            }
        }

        private void ConnectToInputOutputStreams()
        {
            var initializationIsCompleted = false; // TODO: Make this thread-safe? (Interlocked.Exchange etc.)
            _nodeProcessIsReadySource = new TaskCompletionSource<bool>();

            _nodeProcess.OutputDataReceived += (sender, evt) =>
            {
                if (evt.Data == "[Microsoft.AspNetCore.NodeServices:Listening]" && !initializationIsCompleted)
                {
                    _nodeProcessIsReadySource.SetResult(true);
                    initializationIsCompleted = true;
                }
                else if (evt.Data != null)
                {
                    OnOutputDataReceived(evt.Data);
                }
            };

            _nodeProcess.ErrorDataReceived += (sender, evt) =>
            {
                if (evt.Data != null)
                {
                    OnErrorDataReceived(evt.Data);
                    if (!initializationIsCompleted)
                    {
                        _nodeProcessIsReadySource.SetResult(false);
                        initializationIsCompleted = true;
                    }
                }
            };

            _nodeProcess.BeginOutputReadLine();
            _nodeProcess.BeginErrorReadLine();
        }

        protected virtual void OnBeforeLaunchProcess()
        {
        }

        protected virtual void OnOutputDataReceived(string outputData)
        {
            Console.WriteLine("[Node] " + outputData);
        }

        protected virtual void OnErrorDataReceived(string errorData)
        {
            Console.WriteLine("[Node] " + errorData);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _entryPointScript.Dispose();
                }

                ExitNodeProcess();

                _disposed = true;
            }
        }

        ~OutOfProcessNodeInstance()
        {
            Dispose(false);
        }
    }
}