// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.AspNetCore.Razor.TagHelperTool
{
    internal abstract class Client : IDisposable
    {
        // Based on: https://github.com/dotnet/roslyn/blob/14aed138a01c448143b9acf0fe77a662e3dfe2f4/src/Compilers/Shared/BuildServerConnection.cs#L290
        public static async Task<Client> ConnectAsync(string pipeName, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            var timeoutMilliseconds = timeout == null ? Timeout.Infinite : (int)timeout.Value.TotalMilliseconds;

            try
            {
                // Machine-local named pipes are named "\\.\pipe\<pipename>".
                // We use the SHA1 of the directory the compiler exes live in as the pipe name.
                // The NamedPipeClientStream class handles the "\\.\pipe\" part for us.
                CompilerServerLogger.Log("Attempt to open named pipe '{0}'", pipeName);

                var stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                cancellationToken.ThrowIfCancellationRequested();

                CompilerServerLogger.Log("Attempt to connect named pipe '{0}'", pipeName);
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
                    CompilerServerLogger.Log($"Connecting to server timed out after {timeoutMilliseconds} ms");
                    return null;
                }

                CompilerServerLogger.Log("Named pipe '{0}' connected", pipeName);
                cancellationToken.ThrowIfCancellationRequested();

                // The original code in Roslyn checks that the server pipe is owned by the same user for security.
                // We plan to rely on the BCL for this but it's not yet implemented:
                // See https://github.com/dotnet/corefx/issues/25427 

                return new NamedPipeClient(stream);
            }
            catch (Exception e) when (!(e is TaskCanceledException || e is OperationCanceledException))
            {
                CompilerServerLogger.LogException(e, "Exception while connecting to process");
                return null;
            }
        }

        public abstract Stream Stream { get; }

        public void Dispose()
        {
            Dispose(disposing: true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
        private class NamedPipeClient : Client
        {
            public NamedPipeClient(NamedPipeClientStream stream)
            {
                Stream = stream;
            }

            public override Stream Stream { get; }

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
