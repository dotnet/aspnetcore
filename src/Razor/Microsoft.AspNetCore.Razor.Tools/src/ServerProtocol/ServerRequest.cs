// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// After the server pipe is connected, it forks off a thread to handle the connection, and creates
// a new instance of the pipe to listen for new clients. When it gets a request, it validates
// the security and elevation level of the client. If that fails, it disconnects the client. Otherwise,
// it handles the request, sends a response (described by Response class) back to the client, then
// disconnects the pipe and ends the thread.
namespace Microsoft.AspNetCore.Razor.Tools
{
    /// <summary>
    /// Represents a request from the client. A request is as follows.
    /// 
    ///  Field Name         Type                Size (bytes)
    /// ----------------------------------------------------
    ///  Length             Integer             4
    ///  Argument Count     UInteger            4
    ///  Arguments          Argument[]          Variable
    /// 
    /// See <see cref="RequestArgument"/> for the format of an
    /// Argument.
    /// 
    /// </summary>
    internal class ServerRequest
    {
        public ServerRequest(uint protocolVersion, IEnumerable<RequestArgument> arguments)
        {
            ProtocolVersion = protocolVersion;
            Arguments = new ReadOnlyCollection<RequestArgument>(arguments.ToList());

            if (Arguments.Count > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(arguments),
                    $"Too many arguments: maximum of {ushort.MaxValue} arguments allowed.");
            }
        }

        public uint ProtocolVersion { get; }

        public ReadOnlyCollection<RequestArgument> Arguments { get; }

        public TimeSpan? KeepAlive
        {
            get
            {
                TimeSpan? keepAlive = null;
                foreach (var argument in Arguments)
                {
                    if (argument.Id == RequestArgument.ArgumentId.KeepAlive)
                    {
                        // If the value is not a valid integer for any reason, ignore it and continue with the current timeout. 
                        // The client is responsible for validating the argument.
                        if (int.TryParse(argument.Value, out var result))
                        {
                            // Keep alive times are specified in seconds
                            keepAlive = TimeSpan.FromSeconds(result);
                        }
                    }
                }

                return keepAlive;
            }
        }

        public bool IsShutdownRequest()
        {
            return Arguments.Count >= 1 && Arguments[0].Id == RequestArgument.ArgumentId.Shutdown;
        }

        public static ServerRequest Create(
            string workingDirectory,
            string tempDirectory,
            IList<string> args,
            string keepAlive = null)
        {
            ServerLogger.Log("Creating ServerRequest");
            ServerLogger.Log($"Working directory: {workingDirectory}");
            ServerLogger.Log($"Temp directory: {tempDirectory}");

            var requestLength = args.Count + 1;
            var requestArgs = new List<RequestArgument>(requestLength)
            {
                new RequestArgument(RequestArgument.ArgumentId.CurrentDirectory, 0, workingDirectory),
                new RequestArgument(RequestArgument.ArgumentId.TempDirectory, 0, tempDirectory)
            };

            if (keepAlive != null)
            {
                requestArgs.Add(new RequestArgument(RequestArgument.ArgumentId.KeepAlive, 0, keepAlive));
            }

            for (var i = 0; i < args.Count; ++i)
            {
                var arg = args[i];
                ServerLogger.Log($"argument[{i}] = {arg}");
                requestArgs.Add(new RequestArgument(RequestArgument.ArgumentId.CommandLineArgument, i, arg));
            }

            return new ServerRequest(ServerProtocol.ProtocolVersion, requestArgs);
        }

        public static ServerRequest CreateShutdown()
        {
            var requestArgs = new[]
            {
                new RequestArgument(RequestArgument.ArgumentId.Shutdown, argumentIndex: 0, value: ""),
                new RequestArgument(RequestArgument.ArgumentId.CommandLineArgument, argumentIndex: 1, value: "shutdown"),
            };
            return new ServerRequest(ServerProtocol.ProtocolVersion, requestArgs);
        }

        /// <summary>
        /// Read a Request from the given stream.
        /// 
        /// The total request size must be less than 1MB.
        /// </summary>
        /// <returns>null if the Request was too large, the Request otherwise.</returns>
        public static async Task<ServerRequest> ReadAsync(Stream inStream, CancellationToken cancellationToken)
        {
            // Read the length of the request
            var lengthBuffer = new byte[4];
            ServerLogger.Log("Reading length of request");
            await ServerProtocol.ReadAllAsync(inStream, lengthBuffer, 4, cancellationToken).ConfigureAwait(false);
            var length = BitConverter.ToInt32(lengthBuffer, 0);

            // Back out if the request is > 1MB
            if (length > 0x100000)
            {
                ServerLogger.Log("Request is over 1MB in length, cancelling read.");
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Read the full request
            var requestBuffer = new byte[length];
            await ServerProtocol.ReadAllAsync(inStream, requestBuffer, length, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            ServerLogger.Log("Parsing request");
            // Parse the request into the Request data structure.
            using (var reader = new BinaryReader(new MemoryStream(requestBuffer), Encoding.Unicode))
            {
                var protocolVersion = reader.ReadUInt32();
                var argumentCount = reader.ReadUInt32();

                var argumentsBuilder = new List<RequestArgument>((int)argumentCount);

                for (var i = 0; i < argumentCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    argumentsBuilder.Add(RequestArgument.ReadFromBinaryReader(reader));
                }

                return new ServerRequest(protocolVersion, argumentsBuilder);
            }
        }

        /// <summary>
        /// Write a Request to the stream.
        /// </summary>
        public async Task WriteAsync(Stream outStream, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream, Encoding.Unicode))
            {
                // Format the request.
                ServerLogger.Log("Formatting request");
                writer.Write(ProtocolVersion);
                writer.Write(Arguments.Count);
                foreach (var arg in Arguments)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    arg.WriteToBinaryWriter(writer);
                }
                writer.Flush();

                cancellationToken.ThrowIfCancellationRequested();

                // Write the length of the request
                var length = checked((int)memoryStream.Length);

                // Back out if the request is > 1 MB
                if (memoryStream.Length > 0x100000)
                {
                    ServerLogger.Log("Request is over 1MB in length, cancelling write");
                    throw new ArgumentOutOfRangeException();
                }

                // Send the request to the server
                ServerLogger.Log("Writing length of request.");
                await outStream
                    .WriteAsync(BitConverter.GetBytes(length), 0, 4, cancellationToken)
                    .ConfigureAwait(false);

                ServerLogger.Log("Writing request of size {0}", length);
                // Write the request
                memoryStream.Position = 0;
                await memoryStream
                    .CopyToAsync(outStream, bufferSize: length, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}
