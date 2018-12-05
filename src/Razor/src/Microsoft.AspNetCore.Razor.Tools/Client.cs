// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipes;
#if NETFRAMEWORK
using System.Security.AccessControl;
using System.Security.Principal;
#endif
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal abstract class Client : IDisposable
    {
        private static int counter;

        // From https://github.com/dotnet/corefx/blob/29cd6a0b0ac2993cee23ebaf36ca3d4bce6dd75f/src/System.IO.Pipes/ref/System.IO.Pipes.cs#L93.
        // Using the enum value directly as this option is not available in netstandard.
        private const PipeOptions PipeOptionCurrentUserOnly = (PipeOptions)536870912;

        private static readonly PipeOptions _pipeOptions = GetPipeOptions();

        public abstract Stream Stream { get; }

        public abstract string Identifier { get; }

        public void Dispose()
        {
            Dispose(disposing: true);
        }

        public abstract Task WaitForDisconnectAsync(CancellationToken cancellationToken);

        protected virtual void Dispose(bool disposing)
        {
        }

        // Based on: https://github.com/dotnet/roslyn/blob/14aed138a01c448143b9acf0fe77a662e3dfe2f4/src/Compilers/Shared/BuildServerConnection.cs#L290
        public static async Task<Client> ConnectAsync(string pipeName, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var timeoutMilliseconds = timeout == null ? Timeout.Infinite : (int)timeout.Value.TotalMilliseconds;

            try
            {
                // Machine-local named pipes are named "\\.\pipe\<pipename>".
                // We use the SHA1 of the directory the compiler exes live in as the pipe name.
                // The NamedPipeClientStream class handles the "\\.\pipe\" part for us.
                ServerLogger.Log("Attempt to open named pipe '{0}'", pipeName);

                var stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, _pipeOptions);
                cancellationToken.ThrowIfCancellationRequested();

                ServerLogger.Log("Attempt to connect named pipe '{0}'", pipeName);
                try
                {
                    await stream.ConnectAsync(timeoutMilliseconds, cancellationToken);
                }
                catch (Exception e) when (e is IOException || e is TimeoutException)
                {
                    // Note: IOException can also indicate timeout.
                    // From docs:
                    // - TimeoutException: Could not connect to the server within the specified timeout period.
                    // - IOException: The server is connected to another client and the  time-out period has expired.
                    ServerLogger.Log($"Connecting to server timed out after {timeoutMilliseconds} ms");
                    return null;
                }

                ServerLogger.Log("Named pipe '{0}' connected", pipeName);
                cancellationToken.ThrowIfCancellationRequested();

#if NETFRAMEWORK
                // Verify that we own the pipe.
                if (!CheckPipeConnectionOwnership(stream))
                {
                    ServerLogger.Log("Owner of named pipe is incorrect");
                    return null;
                }
#endif

                return new NamedPipeClient(stream, GetNextIdentifier());
            }
            catch (Exception e) when (!(e is TaskCanceledException || e is OperationCanceledException))
            {
                ServerLogger.LogException(e, "Exception while connecting to process");
                return null;
            }
        }

#if NETFRAMEWORK
        /// <summary>
        /// Check to ensure that the named pipe server we connected to is owned by the same
        /// user.
        /// </summary>
        private static bool CheckPipeConnectionOwnership(NamedPipeClientStream pipeStream)
        {
            try
            {
                if (PlatformInformation.IsWindows)
                {
                    using (var currentIdentity = WindowsIdentity.GetCurrent())
                    {
                        var currentOwner = currentIdentity.Owner;
                        var remotePipeSecurity = GetPipeSecurity(pipeStream);
                        var remoteOwner = remotePipeSecurity.GetOwner(typeof(SecurityIdentifier));

                        return currentOwner.Equals(remoteOwner);
                    }
                }

                // We don't need to verify on non-windows as that will be taken care of by the
                // PipeOptions.CurrentUserOnly flag.
                return false;
            }
            catch (Exception ex)
            {
                ServerLogger.LogException(ex, "Checking pipe connection");
                return false;
            }
        }

        private static ObjectSecurity GetPipeSecurity(PipeStream pipeStream)
        {
            return pipeStream.GetAccessControl();
        }
#endif

        private static PipeOptions GetPipeOptions()
        {
            var options = PipeOptions.Asynchronous;

            if (Enum.IsDefined(typeof(PipeOptions), PipeOptionCurrentUserOnly))
            {
                return options | PipeOptionCurrentUserOnly;
            }

            return options;
        }

        private static string GetNextIdentifier()
        {
            var id = Interlocked.Increment(ref counter);
            return "clientconnection-" + id;
        }

        private class NamedPipeClient : Client
        {
            public NamedPipeClient(NamedPipeClientStream stream, string identifier)
            {
                Stream = stream;
                Identifier = identifier;
            }

            public override Stream Stream { get; }

            public override string Identifier { get; }

            public async override Task WaitForDisconnectAsync(CancellationToken cancellationToken)
            {
                if (!(Stream is PipeStream pipeStream))
                {
                    return;
                }

                // We have to poll for disconnection by reading, PipeStream.IsConnected isn't reliable unless you
                // actually do a read - which will cause it to update its state.
                while (!cancellationToken.IsCancellationRequested && pipeStream.IsConnected)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

                    try
                    {
                        ServerLogger.Log($"Before poking pipe {Identifier}.");
                        await Stream.ReadAsync(Array.Empty<byte>(), 0, 0, cancellationToken);
                        ServerLogger.Log($"After poking pipe {Identifier}.");
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception e)
                    {
                        // It is okay for this call to fail.  Errors will be reflected in the
                        // IsConnected property which will be read on the next iteration.
                        ServerLogger.LogException(e, $"Error poking pipe {Identifier}.");
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Stream.Dispose();
                }
            }
        }
    }
}
