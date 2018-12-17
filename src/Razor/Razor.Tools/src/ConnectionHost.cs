// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Tools
{
    // Heavily influenced by:
    // https://github.com/dotnet/roslyn/blob/14aed138a01c448143b9acf0fe77a662e3dfe2f4/src/Compilers/Server/VBCSCompiler/NamedPipeClientConnection.cs#L17
    internal abstract class ConnectionHost
    {
        private static int counter;

        public abstract Task<Connection> WaitForConnectionAsync(CancellationToken cancellationToken);

        public static ConnectionHost Create(string pipeName)
        {
            return new NamedPipeConnectionHost(pipeName);
        }

        private static string GetNextIdentifier()
        {
            var id = Interlocked.Increment(ref counter);
            return "connection-" + id;
        }

        private class NamedPipeConnectionHost : ConnectionHost
        {
            // Size of the buffers to use: 64K
            private const int PipeBufferSize = 0x10000;

            // From https://github.com/dotnet/corefx/blob/29cd6a0b0ac2993cee23ebaf36ca3d4bce6dd75f/src/System.IO.Pipes/ref/System.IO.Pipes.cs#L93.
            // Using the enum value directly as this option is not available in netstandard.
            private const PipeOptions PipeOptionCurrentUserOnly = (PipeOptions)536870912;

            private static readonly PipeOptions _pipeOptions = GetPipeOptions();

            public NamedPipeConnectionHost(string pipeName)
            {
                PipeName = pipeName;
            }

            public string PipeName { get; }

            public async override Task<Connection> WaitForConnectionAsync(CancellationToken cancellationToken)
            {
                // Create the pipe and begin waiting for a connection. This  doesn't block, but could fail 
                // in certain circumstances, such as the OS refusing to create the pipe for some reason 
                // or the pipe was disconnected before we starting listening.
                var pipeStream = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances, // Maximum connections.
                    PipeTransmissionMode.Byte,
                    _pipeOptions,
                    PipeBufferSize, // Default input buffer
                    PipeBufferSize);// Default output buffer

                ServerLogger.Log("Waiting for new connection");
                await pipeStream.WaitForConnectionAsync(cancellationToken);
                ServerLogger.Log("Pipe connection detected.");

                if (Environment.Is64BitProcess || Memory.IsMemoryAvailable())
                {
                    ServerLogger.Log("Memory available - accepting connection");
                    return new NamedPipeConnection(pipeStream, GetNextIdentifier());
                }

                pipeStream.Close();
                throw new Exception("Insufficient resources to process new connection.");
            }

            private static PipeOptions GetPipeOptions()
            {
                var options = PipeOptions.Asynchronous | PipeOptions.WriteThrough;

                if (Enum.IsDefined(typeof(PipeOptions), PipeOptionCurrentUserOnly))
                {
                    return options | PipeOptionCurrentUserOnly;
                }

                return options;
            }
        }

        private class NamedPipeConnection : Connection
        {
            public NamedPipeConnection(NamedPipeServerStream stream, string identifier)
            {
                Stream = stream;
                Identifier = identifier;
            }

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
                ServerLogger.Log($"Pipe {Identifier}: Closing.");

                try
                {
                    Stream.Dispose();
                }
                catch (Exception ex)
                {
                    // The client connection failing to close isn't fatal to the server process.  It is simply a client
                    // for which we can no longer communicate and that's okay because the Close method indicates we are
                    // done with the client already.
                    var message = string.Format($"Pipe {Identifier}: Error closing pipe.");
                    ServerLogger.LogException(ex, message);
                }
            }
        }
    }
}
