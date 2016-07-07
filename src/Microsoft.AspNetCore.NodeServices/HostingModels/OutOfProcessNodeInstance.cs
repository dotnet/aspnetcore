using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

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
        private const string ConnectionEstablishedMessage = "[Microsoft.AspNetCore.NodeServices:Listening]";
        private readonly TaskCompletionSource<object> _connectionIsReadySource = new TaskCompletionSource<object>();
        private bool _disposed;
        private readonly StringAsTempFile _entryPointScript;
        private readonly Process _nodeProcess;

        public OutOfProcessNodeInstance(string entryPointScript, string projectPath, string commandLineArguments = null)
        {
            _entryPointScript = new StringAsTempFile(entryPointScript);
            _nodeProcess = LaunchNodeProcess(_entryPointScript.FileName, projectPath, commandLineArguments);
            ConnectToInputOutputStreams();
        }

        public async Task<T> InvokeExportAsync<T>(string moduleName, string exportNameOrNull, params object[] args)
        {
            // Wait until the connection is established. This will throw if the connection fails to initialize.
            await _connectionIsReadySource.Task;

            if (_nodeProcess.HasExited)
            {
                // This special kind of exception triggers a transparent retry - NodeServicesImpl will launch
                // a new Node instance and pass the invocation to that one instead.
                throw new NodeInvocationException("The Node process has exited", null, nodeInstanceUnavailable: true);
            }

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

                // Make sure the Node process is finished
                // TODO: Is there a more graceful way to end it? Or does this still let it perform any cleanup?
                if (!_nodeProcess.HasExited)
                {
                    _nodeProcess.Kill();
                }

                _disposed = true;
            }
        }

        private static Process LaunchNodeProcess(string entryPointFilename, string projectPath, string commandLineArguments)
        {
            var startInfo = new ProcessStartInfo("node")
            {
                Arguments = "\"" + entryPointFilename + "\" " + (commandLineArguments ?? string.Empty),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = projectPath
            };

            // Append projectPath to NODE_PATH so it can locate node_modules
            var existingNodePath = Environment.GetEnvironmentVariable("NODE_PATH") ?? string.Empty;
            if (existingNodePath != string.Empty)
            {
                existingNodePath += ":";
            }

            var nodePathValue = existingNodePath + Path.Combine(projectPath, "node_modules");
#if NET451
            startInfo.EnvironmentVariables["NODE_PATH"] = nodePathValue;
#else
            startInfo.Environment["NODE_PATH"] = nodePathValue;
#endif

            return Process.Start(startInfo);
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
                    OnOutputDataReceived(evt.Data);
                }
            };

            _nodeProcess.ErrorDataReceived += (sender, evt) =>
            {
                if (evt.Data != null)
                {
                    if (!initializationIsCompleted)
                    {
                        _connectionIsReadySource.SetException(
                            new InvalidOperationException("The Node.js process failed to initialize: " + evt.Data));
                        initializationIsCompleted = true;
                    }
                    else
                    {
                        OnErrorDataReceived(evt.Data);
                    }
                }
            };

            _nodeProcess.BeginOutputReadLine();
            _nodeProcess.BeginErrorReadLine();
        }

        ~OutOfProcessNodeInstance()
        {
            Dispose(false);
        }
    }
}