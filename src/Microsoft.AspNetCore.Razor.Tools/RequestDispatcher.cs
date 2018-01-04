// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.CompilerServer;

namespace Microsoft.AspNetCore.Razor.TagHelperTool
{
    // Heavily influenced by:
    // https://github.com/dotnet/roslyn/blob/14aed138a01c448143b9acf0fe77a662e3dfe2f4/src/Compilers/Server/ServerShared/ServerDispatcher.cs#L15
    internal abstract class RequestDispatcher
    {
        public static RequestDispatcher Create(ConnectionHost connectionHost, CompilerHost compilerHost, CancellationToken cancellationToken)
        {
            return new DefaultRequestDispatcher(connectionHost, compilerHost, cancellationToken);
        }

        /// <summary>
        /// Default time the server will stay alive after the last request disconnects.
        /// </summary>
        public static readonly TimeSpan DefaultServerKeepAlive = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Time to delay after the last connection before initiating a garbage collection
        /// in the server.
        /// </summary>
        public static readonly TimeSpan GCTimeout = TimeSpan.FromSeconds(30);

        public abstract void Run();

        private enum State
        {
            /// <summary>
            /// Server running and accepting all requests
            /// </summary>
            Running,

            /// <summary>
            /// Server processing existing requests, responding to shutdown commands but is not accepting
            /// new build requests.
            /// </summary>
            ShuttingDown,

            /// <summary>
            /// Server is done.
            /// </summary>
            Completed,
        }

        private class DefaultRequestDispatcher : RequestDispatcher
        {
            private readonly CancellationToken _cancellationToken;
            private readonly CompilerHost _compilerHost;
            private readonly ConnectionHost _connectionHost;
            private readonly EventBus _eventBus;

            private KeepAlive _keepAlive;
            private State _state;
            private Task _timeoutTask;
            private Task _gcTask;
            private Task<Connection> _listenTask;
            private CancellationTokenSource _listenCancellationTokenSource;
            private List<Task<ConnectionResult>> _connections = new List<Task<ConnectionResult>>();


            public DefaultRequestDispatcher(ConnectionHost connectionHost, CompilerHost compilerHost, CancellationToken cancellationToken)
            {
                _connectionHost = connectionHost;
                _compilerHost = compilerHost;
                _cancellationToken = cancellationToken;

                _eventBus = EventBus.Default;
                _keepAlive = new KeepAlive(DefaultServerKeepAlive, isDefault: true);
            }

            // The server accepts connections until we reach a state that requires a shutdown. At that
            // time no new connections will be accepted and the server will drain existing connections.
            //
            // The idea is that it's better to let clients fallback to in-proc (and slow down) than it is to keep
            // running in an undesired state.
            public override void Run()
            {
                _state = State.Running;

                try
                {
                    Listen();

                    do
                    {
                        Debug.Assert(_listenTask != null);

                        MaybeCreateTimeoutTask();
                        MaybeCreateGCTask();
                        WaitForAnyCompletion(_cancellationToken);
                        CheckCompletedTasks(_cancellationToken);
                    }
                    while (_connections.Count > 0 || _state == State.Running);
                }
                finally
                {
                    _state = State.Completed;
                    _gcTask = null;
                    _timeoutTask = null;

                    if (_listenTask != null)
                    {
                        CloseListenTask();
                    }
                }
            }


            private void CheckCompletedTasks(CancellationToken cancellationToken)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    HandleCancellation();
                    return;
                }

                if (_listenTask.IsCompleted)
                {
                    HandleCompletedListenTask(cancellationToken);
                }

                if (_timeoutTask?.IsCompleted == true)
                {
                    HandleCompletedTimeoutTask();
                }

                if (_gcTask?.IsCompleted == true)
                {
                    HandleCompletedGCTask();
                }

                HandleCompletedConnections();
            }

            private void HandleCancellation()
            {
                Debug.Assert(_listenTask != null);

                // If cancellation has been requested then the server needs to be in the process
                // of shutting down.
                _state = State.ShuttingDown;

                CloseListenTask();

                try
                {
                    Task.WaitAll(_connections.ToArray());
                }
                catch
                {
                    // It's expected that some will throw exceptions, in particular OperationCanceledException.  It's
                    // okay for them to throw so long as they complete.
                }

                HandleCompletedConnections();
                Debug.Assert(_connections.Count == 0);
            }

            /// <summary>
            /// The server farms out work to Task values and this method needs to wait until at least one of them
            /// has completed.
            /// </summary>
            private void WaitForAnyCompletion(CancellationToken cancellationToken)
            {
                var all = new List<Task>();
                all.AddRange(_connections);
                all.Add(_timeoutTask);
                all.Add(_listenTask);
                all.Add(_gcTask);

                try
                {
                    var waitArray = all.Where(x => x != null).ToArray();
                    Task.WaitAny(waitArray, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Thrown when the provided cancellationToken is cancelled.  This is handled in the caller,
                    // here it just serves to break out of the WaitAny call.
                }
            }

            private void Listen()
            {
                Debug.Assert(_listenTask == null);
                Debug.Assert(_timeoutTask == null);

                _listenCancellationTokenSource = new CancellationTokenSource();
                _listenTask = _connectionHost.WaitForConnectionAsync(_listenCancellationTokenSource.Token);
                _eventBus.ConnectionListening();
            }

            private void CloseListenTask()
            {
                Debug.Assert(_listenTask != null);

                _listenCancellationTokenSource.Cancel();
                _listenCancellationTokenSource = null;
                _listenTask = null;
            }

            private void HandleCompletedListenTask(CancellationToken cancellationToken)
            {
                _eventBus.ConnectionReceived();

                // Don't accept any new connections once we're in shutdown mode, instead gracefully reject the request.
                // This should cause the client to run in process.
                var accept = _state == State.Running;
                var connectionTask = AcceptConnection(_listenTask, accept, cancellationToken);
                _connections.Add(connectionTask);

                // Timeout and GC are only done when there are no active connections.  Now that we have a new
                // connection cancel out these tasks.
                _timeoutTask = null;
                _gcTask = null;

                // Begin listening again for new connections.
                _listenTask = null;
                Listen();
            }

            private void HandleCompletedTimeoutTask()
            {
                _eventBus.KeepAliveReached();
                _listenCancellationTokenSource.Cancel();
                _timeoutTask = null;
                _state = State.ShuttingDown;
            }

            private void HandleCompletedGCTask()
            {
                _gcTask = null;
                for (int i = 0; i < 10; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
            }

            private void MaybeCreateTimeoutTask()
            {
                // If there are no active clients running then the server needs to be in a timeout mode.
                if (_connections.Count == 0 && _timeoutTask == null)
                {
                    Debug.Assert(_listenTask != null);
                    _timeoutTask = Task.Delay(_keepAlive.TimeSpan);
                }
            }

            private void MaybeCreateGCTask()
            {
                if (_connections.Count == 0 && _gcTask == null)
                {
                    _gcTask = Task.Delay(GCTimeout);
                }
            }

            /// <summary>
            /// Checks the completed connection objects.
            /// </summary>
            /// <returns>False if the server needs to begin shutting down</returns>
            private void HandleCompletedConnections()
            {
                var shutdown = false;
                var processedCount = 0;
                var i = 0;
                while (i < _connections.Count)
                {
                    var current = _connections[i];
                    if (!current.IsCompleted)
                    {
                        i++;
                        continue;
                    }

                    _connections.RemoveAt(i);
                    processedCount++;

                    var result = current.Result;
                    if (result.KeepAlive.HasValue)
                    {
                        var updated = _keepAlive.Update(result.KeepAlive.Value);
                        if (updated.Equals(_keepAlive))
                        {
                            _eventBus.UpdateKeepAlive(updated.TimeSpan);
                        }
                    }

                    switch (result.CloseReason)
                    {
                        case ConnectionResult.Reason.CompilationCompleted:
                        case ConnectionResult.Reason.CompilationNotStarted:
                            // These are all normal end states.  Nothing to do here.
                            break;

                        case ConnectionResult.Reason.ClientDisconnect:
                            // Have to assume the worst here which is user pressing Ctrl+C at the command line and
                            // hence wanting all compilation to end.
                            _eventBus.ConnectionRudelyEnded();
                            shutdown = true;
                            break;

                        case ConnectionResult.Reason.ClientException:
                        case ConnectionResult.Reason.ClientShutdownRequest:
                            _eventBus.ConnectionRudelyEnded();
                            shutdown = true;
                            break;

                        default:
                            throw new InvalidOperationException($"Unexpected enum value {result.CloseReason}");
                    }
                }

                if (processedCount > 0)
                {
                    _eventBus.ConnectionCompleted(processedCount);
                }

                if (shutdown)
                {
                    _state = State.ShuttingDown;
                }
            }

            internal async Task<ConnectionResult> AcceptConnection(Task<Connection> task, bool accept, CancellationToken cancellationToken)
            {
                Connection connection;
                try
                {
                    connection = await task;
                }
                catch (Exception ex)
                {
                    // Unable to establish a connection with the client.  The client is responsible for
                    // handling this case.  Nothing else for us to do here.
                    CompilerServerLogger.LogException(ex, "Error creating client named pipe");
                    return new ConnectionResult(ConnectionResult.Reason.CompilationNotStarted);
                }

                try
                {
                    using (connection)
                    { 
                        BuildRequest request;
                        try
                        {
                            CompilerServerLogger.Log("Begin reading request.");
                            request = await BuildRequest.ReadAsync(connection.Stream, cancellationToken).ConfigureAwait(false);
                            CompilerServerLogger.Log("End reading request.");
                        }
                        catch (Exception e)
                        {
                            CompilerServerLogger.LogException(e, "Error reading build request.");
                            return new ConnectionResult(ConnectionResult.Reason.CompilationNotStarted);
                        }

                        if (request.IsShutdownRequest())
                        {
                            // Reply with the PID of this process so that the client can wait for it to exit.
                            var response = new ShutdownBuildResponse(Process.GetCurrentProcess().Id);
                            await response.WriteAsync(connection.Stream, cancellationToken);

                            // We can safely disconnect the client, then when this connection gets cleaned up by the event loop
                            // the server will go to a shutdown state.
                            return new ConnectionResult(ConnectionResult.Reason.ClientShutdownRequest);
                        }
                        else if (!accept)
                        {
                            // We're already in shutdown mode, respond gracefully so the client can run in-process.
                            var response = new RejectedBuildResponse();
                            await response.WriteAsync(connection.Stream, cancellationToken).ConfigureAwait(false);

                            return new ConnectionResult(ConnectionResult.Reason.CompilationNotStarted);
                        }
                        else
                        {
                            // If we get here then this is a real request that we will accept and process.
                            //
                            // Kick off both the compilation and a task to monitor the pipe for closing.
                            var buildCancelled = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                            var watcher = connection.WaitForDisconnectAsync(buildCancelled.Token);
                            var worker = ExecuteRequestAsync(request, buildCancelled.Token);

                            // await will end when either the work is complete or the connection is closed.
                            await Task.WhenAny(worker, watcher);

                            // Do an 'await' on the completed task, preference being compilation, to force
                            // any exceptions to be realized in this method for logging.
                            ConnectionResult.Reason reason;
                            if (worker.IsCompleted)
                            {
                                var response = await worker;

                                try
                                {
                                    CompilerServerLogger.Log("Begin writing response.");
                                    await response.WriteAsync(connection.Stream, cancellationToken);
                                    CompilerServerLogger.Log("End writing response.");
                                    
                                    reason = ConnectionResult.Reason.CompilationCompleted;
                                }
                                catch
                                {
                                    reason = ConnectionResult.Reason.ClientDisconnect;
                                }
                            }
                            else
                            {
                                await watcher;
                                reason = ConnectionResult.Reason.ClientDisconnect;
                            }

                            // Begin the tear down of the Task which didn't complete.
                            buildCancelled.Cancel();
                            
                            return new ConnectionResult(reason, request.KeepAlive);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CompilerServerLogger.LogException(ex, "Error handling connection");
                    return new ConnectionResult(ConnectionResult.Reason.ClientException);
                }
            }

            private Task<BuildResponse> ExecuteRequestAsync(BuildRequest buildRequest, CancellationToken cancellationToken)
            {
                Func<BuildResponse> func = () =>
                {
                    CompilerServerLogger.Log("Begin processing request");


                    // TODO: this is where we actually process the request.
                    // Take a look at BuildProtocolUtil
                    var response = (BuildResponse)null;

                    CompilerServerLogger.Log("End processing request");
                    return response;
                };

                var task = new Task<BuildResponse>(func, cancellationToken, TaskCreationOptions.LongRunning);
                task.Start();
                return task;
            }
        }

        private struct KeepAlive
        {
            public TimeSpan TimeSpan;
            public bool IsDefault;

            public KeepAlive(TimeSpan timeSpan, bool isDefault)
            {
                TimeSpan = timeSpan;
                IsDefault = isDefault;
            }

            public KeepAlive Update(TimeSpan timeSpan)
            {
                if (IsDefault || timeSpan > TimeSpan)
                {
                    return new KeepAlive(timeSpan, isDefault: false);
                }

                return this;
            }
        }
    }
}
