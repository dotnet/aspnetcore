// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tools
{
    public class ServerLifecycleTest
    {
        private static ServerRequest EmptyServerRequest => new ServerRequest(1, Array.Empty<RequestArgument>());

        private static ServerResponse EmptyServerResponse => new CompletedServerResponse(
            returnCode: 0,
            utf8output: false,
            output: string.Empty);

        [Fact]
        public void ServerStartup_MutexAlreadyAcquired_Fails()
        {
            // Arrange
            var pipeName = Guid.NewGuid().ToString("N");
            var mutexName = MutexName.GetServerMutexName(pipeName);
            var compilerHost = new Mock<CompilerHost>(MockBehavior.Strict);
            var host = new Mock<ConnectionHost>(MockBehavior.Strict);

            // Act & Assert
            using (var mutex = new Mutex(initiallyOwned: true, name: mutexName, createdNew: out var holdsMutex))
            {
                Assert.True(holdsMutex);
                try
                {
                    var result = ServerUtilities.RunServer(pipeName, host.Object, compilerHost.Object);

                    // Assert failure
                    Assert.Equal(1, result);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        [Fact]
        public void ServerStartup_SuccessfullyAcquiredMutex()
        {
            // Arrange, Act & Assert
            var pipeName = Guid.NewGuid().ToString("N");
            var mutexName = MutexName.GetServerMutexName(pipeName);
            var compilerHost = new Mock<CompilerHost>(MockBehavior.Strict);
            var host = new Mock<ConnectionHost>(MockBehavior.Strict);
            host
                .Setup(x => x.WaitForConnectionAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    // Use a thread instead of Task to guarantee this code runs on a different
                    // thread and we can validate the mutex state. 
                    var source = new TaskCompletionSource<bool>();
                    var thread = new Thread(_ =>
                    {
                        Mutex mutex = null;
                        try
                        {
                            Assert.True(Mutex.TryOpenExisting(mutexName, out mutex));
                            Assert.False(mutex.WaitOne(millisecondsTimeout: 0));
                            source.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            source.SetException(ex);
                            throw;
                        }
                        finally
                        {
                            mutex?.Dispose();
                        }
                    });

                    // Synchronously wait here.  Don't returned a Task value because we need to 
                    // ensure the above check completes before the server hits a timeout and 
                    // releases the mutex. 
                    thread.Start();
                    source.Task.Wait();

                    return new TaskCompletionSource<Connection>().Task;
                });

            var result = ServerUtilities.RunServer(pipeName, host.Object, compilerHost.Object, keepAlive: TimeSpan.FromSeconds(1));
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task ServerRunning_ShutdownRequest_processesSuccessfully()
        {
            // Arrange
            using (var serverData = ServerUtilities.CreateServer())
            {
                // Act
                var serverProcessId = await ServerUtilities.SendShutdown(serverData.PipeName);

                // Assert
                Assert.Equal(Process.GetCurrentProcess().Id, serverProcessId);
                await serverData.Verify(connections: 1, completed: 1);
            }
        }

        /// <summary>
        /// A shutdown request should not abort an existing compilation.  It should be allowed to run to 
        /// completion.
        /// </summary>
        [Fact]
        public async Task ServerRunning_ShutdownRequest_DoesNotAbortCompilation()
        {
            // Arrange
            var startCompilationSource = new TaskCompletionSource<bool>();
            var finishCompilationSource = new TaskCompletionSource<bool>();
            var host = CreateCompilerHost(c => c.ExecuteFunc = (req, ct) =>
            {
                // At this point, the connection has been accepted and the compilation has started.
                startCompilationSource.SetResult(true);

                // We want this to keep running even after the shutdown is seen.
                finishCompilationSource.Task.Wait();
                return EmptyServerResponse;
            });

            using (var serverData = ServerUtilities.CreateServer(compilerHost: host))
            {
                var compileTask = ServerUtilities.Send(serverData.PipeName, EmptyServerRequest);

                // Wait for the request to go through and trigger compilation.
                await startCompilationSource.Task;

                // Act
                // The compilation is now in progress, send the shutdown.
                await ServerUtilities.SendShutdown(serverData.PipeName);
                Assert.False(compileTask.IsCompleted);

                // Now let the task complete.
                finishCompilationSource.SetResult(true);

                // Assert
                var response = await compileTask;
                Assert.Equal(ServerResponse.ResponseType.Completed, response.Type);
                Assert.Equal(0, ((CompletedServerResponse)response).ReturnCode);

                await serverData.Verify(connections: 2, completed: 2);
            }
        }

        /// <summary>
        /// Multiple clients should be able to send shutdown requests to the server.
        /// </summary>
        [Fact]
        public async Task ServerRunning_MultipleShutdownRequests_HandlesSuccessfully()
        {
            // Arrange
            var startCompilationSource = new TaskCompletionSource<bool>();
            var finishCompilationSource = new TaskCompletionSource<bool>();
            var host = CreateCompilerHost(c => c.ExecuteFunc = (req, ct) =>
            {
                // At this point, the connection has been accepted and the compilation has started.
                startCompilationSource.SetResult(true);

                // We want this to keep running even after the shutdown is seen.
                finishCompilationSource.Task.Wait();
                return EmptyServerResponse;
            });

            using (var serverData = ServerUtilities.CreateServer(compilerHost: host))
            {
                var compileTask = ServerUtilities.Send(serverData.PipeName, EmptyServerRequest);

                // Wait for the request to go through and trigger compilation.
                await startCompilationSource.Task;

                // Act
                for (var i = 0; i < 10; i++)
                {
                    // The compilation is now in progress, send the shutdown.
                    var processId = await ServerUtilities.SendShutdown(serverData.PipeName);
                    Assert.Equal(Process.GetCurrentProcess().Id, processId);
                    Assert.False(compileTask.IsCompleted);
                }

                // Now let the task complete.
                finishCompilationSource.SetResult(true);

                // Assert
                var response = await compileTask;
                Assert.Equal(ServerResponse.ResponseType.Completed, response.Type);
                Assert.Equal(0, ((CompletedServerResponse)response).ReturnCode);

                await serverData.Verify(connections: 11, completed: 11);
            }
        }

        [ConditionalFact(Skip = "https://github.com/aspnet/Razor/issues/1991")]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public async Task ServerRunning_CancelCompilation_CancelsSuccessfully()
        {
            // Arrange
            const int requestCount = 5;
            var count = 0;
            var completionSource = new TaskCompletionSource<bool>();
            var host = CreateCompilerHost(c => c.ExecuteFunc = (req, ct) =>
            {
                if (Interlocked.Increment(ref count) == requestCount)
                {
                    completionSource.SetResult(true);
                }

                ct.WaitHandle.WaitOne();
                return new RejectedServerResponse();
            });

            var semaphore = new SemaphoreSlim(1);
            Action<object, EventArgs> onListening = (s, e) =>
            {
                semaphore.Release();
            };
            using (var serverData = ServerUtilities.CreateServer(compilerHost: host, onListening: onListening))
            {
                // Send all the requests.
                var clients = new List<Client>();
                for (var i = 0; i < requestCount; i++)
                {
                    // Wait for the server to start listening.
                    await semaphore.WaitAsync(TimeSpan.FromMinutes(1));

                    var client = await Client.ConnectAsync(serverData.PipeName, timeout: null, cancellationToken: default);
                    await EmptyServerRequest.WriteAsync(client.Stream);
                    clients.Add(client);
                }

                // Act
                // Wait until all of the connections are being processed by the server. 
                await completionSource.Task;

                // Now cancel
                var stats = await serverData.CancelAndCompleteAsync();

                // Assert
                Assert.Equal(requestCount, stats.Connections);
                Assert.Equal(requestCount, count);

                // Read the server response to each client.
                foreach (var client in clients)
                {
                    var task = ServerResponse.ReadAsync(client.Stream);
                    // We expect this to throw because the stream is already closed.
                    await Assert.ThrowsAnyAsync<IOException>(() => task);
                    client.Dispose();
                }
            }
        }

        private static TestableCompilerHost CreateCompilerHost(Action<TestableCompilerHost> configureCompilerHost = null)
        {
            var compilerHost = new TestableCompilerHost();
            configureCompilerHost?.Invoke(compilerHost);

            return compilerHost;
        }

        private class TestableCompilerHost : CompilerHost
        {
            internal Func<ServerRequest, CancellationToken, ServerResponse> ExecuteFunc;

            public override ServerResponse Execute(ServerRequest request, CancellationToken cancellationToken)
            {
                if (ExecuteFunc != null)
                {
                    return ExecuteFunc(request, cancellationToken);
                }

                return EmptyServerResponse;
            }
        }
    }
}
