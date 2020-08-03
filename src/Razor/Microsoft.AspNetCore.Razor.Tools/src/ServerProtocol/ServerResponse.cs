// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
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
    /// Base class for all possible responses to a request.
    /// The ResponseType enum should list all possible response types
    /// and ReadResponse creates the appropriate response subclass based
    /// on the response type sent by the client.
    /// The format of a response is:
    ///
    /// Field Name       Field Type          Size (bytes)
    /// -------------------------------------------------
    /// responseLength   int (positive)      4  
    /// responseType     enum ResponseType   4
    /// responseBody     Response subclass   variable
    /// </summary>
    internal abstract class ServerResponse
    {
        public enum ResponseType
        {
            // The client and server are using incompatible protocol versions.
            MismatchedVersion,

            // The build request completed on the server and the results are contained
            // in the message. 
            Completed,

            // The shutdown request completed and the server process information is 
            // contained in the message. 
            Shutdown,

            // The request was rejected by the server.  
            Rejected,
        }

        public abstract ResponseType Type { get; }

        public async Task WriteAsync(Stream outStream, CancellationToken cancellationToken)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream, Encoding.Unicode))
            {
                // Format the response
                ServerLogger.Log("Formatting Response");
                writer.Write((int)Type);

                AddResponseBody(writer);
                writer.Flush();

                cancellationToken.ThrowIfCancellationRequested();

                // Send the response to the client

                // Write the length of the response
                var length = checked((int)memoryStream.Length);

                ServerLogger.Log("Writing response length");
                // There is no way to know the number of bytes written to
                // the pipe stream. We just have to assume all of them are written.
                await outStream
                    .WriteAsync(BitConverter.GetBytes(length), 0, 4, cancellationToken)
                    .ConfigureAwait(false);

                // Write the response
                ServerLogger.Log("Writing response of size {0}", length);
                memoryStream.Position = 0;
                await memoryStream
                    .CopyToAsync(outStream, bufferSize: length, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        protected abstract void AddResponseBody(BinaryWriter writer);

        /// <summary>
        /// May throw exceptions if there are pipe problems.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<ServerResponse> ReadAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            ServerLogger.Log("Reading response length");
            // Read the response length
            var lengthBuffer = new byte[4];
            await ServerProtocol.ReadAllAsync(stream, lengthBuffer, 4, cancellationToken).ConfigureAwait(false);
            var length = BitConverter.ToUInt32(lengthBuffer, 0);

            // Read the response
            ServerLogger.Log("Reading response of length {0}", length);
            var responseBuffer = new byte[length];
            await ServerProtocol.ReadAllAsync(
                stream,
                responseBuffer,
                responseBuffer.Length,
                cancellationToken)
                .ConfigureAwait(false);

            using (var reader = new BinaryReader(new MemoryStream(responseBuffer), Encoding.Unicode))
            {
                var responseType = (ResponseType)reader.ReadInt32();

                switch (responseType)
                {
                    case ResponseType.Completed:
                        return CompletedServerResponse.Create(reader);
                    case ResponseType.MismatchedVersion:
                        return new MismatchedVersionServerResponse();
                    case ResponseType.Shutdown:
                        return ShutdownServerResponse.Create(reader);
                    case ResponseType.Rejected:
                        return new RejectedServerResponse();
                    default:
                        throw new InvalidOperationException("Received invalid response type from server.");
                }
            }
        }
    }
}
