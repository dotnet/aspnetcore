using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNet.NodeServices {
    /**
     * Class responsible for launching the Node child process, determining when it is ready to accept invocations,
     * and finally killing it when the parent process exits. Also it restarts the child process if it dies.
     */
    internal abstract class OutOfProcessNodeRunner : NodeHost {
        private object _childProcessLauncherLock;
        private bool disposed;
        private StringAsTempFile _entryPointScript;
        private string _commandLineArguments;
        private Process _nodeProcess;
        private TaskCompletionSource<bool> _nodeProcessIsReadySource;
        
        protected Process NodeProcess {
            get {
                // This is only exposed to support the UnreliableStreamNodeHost, which is just to verify that
                // other hosting/transport mechanisms are possible. This shouldn't really be exposed.
                return this._nodeProcess;
            }
        }
        
        public OutOfProcessNodeRunner(string entryPointScript, string commandLineArguments = null)
        {
            this._childProcessLauncherLock = new object();
            this._entryPointScript = new StringAsTempFile(entryPointScript);
            this._commandLineArguments = commandLineArguments ?? string.Empty;
        }
        
        protected async Task EnsureReady() {
            lock (this._childProcessLauncherLock) {
                if (this._nodeProcess == null || this._nodeProcess.HasExited) {
                    var startInfo = new ProcessStartInfo("node") {
                        Arguments = this._entryPointScript.FileName + " " + this._commandLineArguments,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    
                    // Append current directory to NODE_PATH so it can locate node_modules
                    var existingNodePath = Environment.GetEnvironmentVariable("NODE_PATH") ?? string.Empty;
                    if (existingNodePath != string.Empty) {
                        existingNodePath += ":";
                    }
                    
                    var nodePathValue = existingNodePath + Path.Combine(Directory.GetCurrentDirectory(), "node_modules");
                    #if DNX451
                    startInfo.EnvironmentVariables.Add("NODE_PATH", nodePathValue);
                    #else
                    startInfo.Environment.Add("NODE_PATH", nodePathValue);
                    #endif
                    
                    this.OnBeforeLaunchProcess();
                    this._nodeProcess = Process.Start(startInfo);
                    this.ConnectToInputOutputStreams();
                }
            }
            
            var initializationSucceeded = await this._nodeProcessIsReadySource.Task;
            if (!initializationSucceeded) {
                throw new InvalidOperationException("The Node.js process failed to initialize");
            }
        }
        
        private void ConnectToInputOutputStreams() {
            var initializationIsCompleted = false; // TODO: Make this thread-safe? (Interlocked.Exchange etc.)
            this._nodeProcessIsReadySource = new TaskCompletionSource<bool>();
            
            this._nodeProcess.OutputDataReceived += (sender, evt) => {
                if (evt.Data == "[Microsoft.AspNet.NodeServices:Listening]" && !initializationIsCompleted) {
                    this._nodeProcessIsReadySource.SetResult(true);
                    initializationIsCompleted = true;
                } else if (evt.Data != null) {
                    this.OnOutputDataReceived(evt.Data);
                }
            };

            this._nodeProcess.ErrorDataReceived += (sender, evt) => {
                if (evt.Data != null) {
                    this.OnErrorDataReceived(evt.Data);
                    if (!initializationIsCompleted) {
                        this._nodeProcessIsReadySource.SetResult(false);
                        initializationIsCompleted = true;
                    }
                }
            };
                                
            this._nodeProcess.BeginOutputReadLine();
            this._nodeProcess.BeginErrorReadLine();            
        }
        
        protected virtual void OnBeforeLaunchProcess() {
        }

        protected virtual void OnOutputDataReceived(string outputData) {
            Console.WriteLine("[Node] " + outputData);
        }
        
        protected virtual void OnErrorDataReceived(string errorData) {
            Console.WriteLine("[Node] " + errorData);
        }
        
        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed) {
                if (disposing) {
                    this._entryPointScript.Dispose();
                }
                
                if (this._nodeProcess != null && !this._nodeProcess.HasExited) {
                    this._nodeProcess.Kill(); // TODO: Is there a more graceful way to end it? Or does this still let it perform any cleanup?                    System.Console.WriteLine("Killed");
                }
                
                disposed = true;
            }
        }
    
        ~OutOfProcessNodeRunner() {
            Dispose (false);
        }
    }
}