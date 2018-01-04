// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CommandLine.BuildProtocolConstants;
using static Microsoft.CodeAnalysis.CommandLine.CompilerServerLogger;

// This file describes data structures about the protocol from client program to server that is 
// used. The basic protocol is this.
//
// After the server pipe is connected, it forks off a thread to handle the connection, and creates
// a new instance of the pipe to listen for new clients. When it gets a request, it validates
// the security and elevation level of the client. If that fails, it disconnects the client. Otherwise,
// it handles the request, sends a response (described by Response class) back to the client, then
// disconnects the pipe and ends the thread.

namespace Microsoft.CodeAnalysis.CommandLine
{
    /// <summary>
    /// Represents a request from the client. A request is as follows.
    /// 
    ///  Field Name         Type                Size (bytes)
    /// ----------------------------------------------------
    ///  Length             Integer             4
    ///  Language           RequestLanguage     4
    ///  Argument Count     UInteger            4
    ///  Arguments          Argument[]          Variable
    /// 
    /// See <see cref="Argument"/> for the format of an
    /// Argument.
    /// 
    /// </summary>
    internal class BuildRequest
    {
        public readonly uint ProtocolVersion;
        public readonly RequestLanguage Language;
        public readonly ReadOnlyCollection<Argument> Arguments;

        public BuildRequest(uint protocolVersion, RequestLanguage language, IEnumerable<Argument> arguments)
        {
            ProtocolVersion = protocolVersion;
            Language = language;
            Arguments = new ReadOnlyCollection<Argument>(arguments.ToList());

            if (Arguments.Count > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(arguments),
                    $"Too many arguments: maximum of {ushort.MaxValue} arguments allowed.");
            }
        }
        
        public TimeSpan? KeepAlive
        {
            get
            {
                TimeSpan? keepAlive = null;
                foreach (var argument in Arguments)
                {
                    if (argument.ArgumentId == BuildProtocolConstants.ArgumentId.KeepAlive)
                    {
                        // If the value is not a valid integer for any reason,ignore it and continue with the current timeout. 
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

        public static BuildRequest Create(
            RequestLanguage language,
            string workingDirectory,
            string tempDirectory,
            IList<string> args,
            string keepAlive = null,
            string libDirectory = null)
        {
            Log("Creating BuildRequest");
            Log($"Working directory: {workingDirectory}");
            Log($"Temp directory: {tempDirectory}");
            Log($"Lib directory: {libDirectory ?? "null"}");

            var requestLength = args.Count + 1 + (libDirectory == null ? 0 : 1);
            var requestArgs = new List<Argument>(requestLength);

            requestArgs.Add(new Argument(ArgumentId.CurrentDirectory, 0, workingDirectory));
            requestArgs.Add(new Argument(ArgumentId.TempDirectory, 0, tempDirectory));

            if (keepAlive != null)
            {
                requestArgs.Add(new Argument(ArgumentId.KeepAlive, 0, keepAlive));
            }

            if (libDirectory != null)
            {
                requestArgs.Add(new Argument(ArgumentId.LibEnvVariable, 0, libDirectory));
            }

            for (int i = 0; i < args.Count; ++i)
            {
                var arg = args[i];
                Log($"argument[{i}] = {arg}");
                requestArgs.Add(new Argument(ArgumentId.CommandLineArgument, i, arg));
            }

            return new BuildRequest(BuildProtocolConstants.ProtocolVersion, language, requestArgs);
        }

        public static BuildRequest CreateShutdown()
        {
            var requestArgs = new[] { new Argument(ArgumentId.Shutdown, argumentIndex: 0, value: "") };
            return new BuildRequest(BuildProtocolConstants.ProtocolVersion, RequestLanguage.CSharpCompile, requestArgs);
        }

        public bool IsShutdownRequest()
        {
            return Arguments.Count == 1 && Arguments[0].ArgumentId == ArgumentId.Shutdown;
        }

        /// <summary>
        /// Read a Request from the given stream.
        /// 
        /// The total request size must be less than 1MB.
        /// </summary>
        /// <returns>null if the Request was too large, the Request otherwise.</returns>
        public static async Task<BuildRequest> ReadAsync(Stream inStream, CancellationToken cancellationToken)
        {
            // Read the length of the request
            var lengthBuffer = new byte[4];
            Log("Reading length of request");
            await ReadAllAsync(inStream, lengthBuffer, 4, cancellationToken).ConfigureAwait(false);
            var length = BitConverter.ToInt32(lengthBuffer, 0);

            // Back out if the request is > 1MB
            if (length > 0x100000)
            {
                Log("Request is over 1MB in length, cancelling read.");
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Read the full request
            var requestBuffer = new byte[length];
            await ReadAllAsync(inStream, requestBuffer, length, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            Log("Parsing request");
            // Parse the request into the Request data structure.
            using (var reader = new BinaryReader(new MemoryStream(requestBuffer), Encoding.Unicode))
            {
                var protocolVersion = reader.ReadUInt32();
                var language = (RequestLanguage)reader.ReadUInt32();
                uint argumentCount = reader.ReadUInt32();

                var argumentsBuilder = new List<Argument>((int)argumentCount);

                for (int i = 0; i < argumentCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    argumentsBuilder.Add(BuildRequest.Argument.ReadFromBinaryReader(reader));
                }

                return new BuildRequest(protocolVersion,
                                        language,
                                        argumentsBuilder);
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
                Log("Formatting request");
                writer.Write(ProtocolVersion);
                writer.Write((uint)Language);
                writer.Write(Arguments.Count);
                foreach (Argument arg in Arguments)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    arg.WriteToBinaryWriter(writer);
                }
                writer.Flush();

                cancellationToken.ThrowIfCancellationRequested();

                // Write the length of the request
                int length = checked((int)memoryStream.Length);

                // Back out if the request is > 1 MB
                if (memoryStream.Length > 0x100000)
                {
                    Log("Request is over 1MB in length, cancelling write");
                    throw new ArgumentOutOfRangeException();
                }

                // Send the request to the server
                Log("Writing length of request.");
                await outStream.WriteAsync(BitConverter.GetBytes(length), 0, 4,
                                           cancellationToken).ConfigureAwait(false);

                Log("Writing request of size {0}", length);
                // Write the request
                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(outStream, bufferSize: length, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// A command line argument to the compilation. 
        /// An argument is formatted as follows:
        /// 
        ///  Field Name         Type            Size (bytes)
        /// --------------------------------------------------
        ///  ID                 UInteger        4
        ///  Index              UInteger        4
        ///  Value              String          Variable
        /// 
        /// Strings are encoded via a length prefix as a signed
        /// 32-bit integer, followed by an array of characters.
        /// </summary>
        public struct Argument
        {
            public readonly ArgumentId ArgumentId;
            public readonly int ArgumentIndex;
            public readonly string Value;

            public Argument(ArgumentId argumentId,
                            int argumentIndex,
                            string value)
            {
                ArgumentId = argumentId;
                ArgumentIndex = argumentIndex;
                Value = value;
            }

            public static Argument ReadFromBinaryReader(BinaryReader reader)
            {
                var argId = (ArgumentId)reader.ReadInt32();
                var argIndex = reader.ReadInt32();
                string value = ReadLengthPrefixedString(reader);
                return new Argument(argId, argIndex, value);
            }

            public void WriteToBinaryWriter(BinaryWriter writer)
            {
                writer.Write((int)ArgumentId);
                writer.Write(ArgumentIndex);
                WriteLengthPrefixedString(writer, Value);
            }
        }
    }

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
    internal abstract class BuildResponse
    {
        public enum ResponseType
        {
            // The client and server are using incompatible protocol versions.
            MismatchedVersion,

            // The build request completed on the server and the results are contained
            // in the message. 
            Completed,

            // The build request could not be run on the server due because it created
            // an unresolvable inconsistency with analyzers.  
            AnalyzerInconsistency,

            // The shutdown request completed and the server process information is 
            // contained in the message. 
            Shutdown,

            // The request was rejected by the server.  
            Rejected,
        }

        public abstract ResponseType Type { get; }

        public async Task WriteAsync(Stream outStream,
                               CancellationToken cancellationToken)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream, Encoding.Unicode))
            {
                // Format the response
                Log("Formatting Response");
                writer.Write((int)Type);

                AddResponseBody(writer);
                writer.Flush();

                cancellationToken.ThrowIfCancellationRequested();

                // Send the response to the client

                // Write the length of the response
                int length = checked((int)memoryStream.Length);

                Log("Writing response length");
                // There is no way to know the number of bytes written to
                // the pipe stream. We just have to assume all of them are written.
                await outStream.WriteAsync(BitConverter.GetBytes(length),
                                           0,
                                           4,
                                           cancellationToken).ConfigureAwait(false);

                // Write the response
                Log("Writing response of size {0}", length);
                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(outStream, bufferSize: length, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        protected abstract void AddResponseBody(BinaryWriter writer);

        /// <summary>
        /// May throw exceptions if there are pipe problems.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<BuildResponse> ReadAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            Log("Reading response length");
            // Read the response length
            var lengthBuffer = new byte[4];
            await ReadAllAsync(stream, lengthBuffer, 4, cancellationToken).ConfigureAwait(false);
            var length = BitConverter.ToUInt32(lengthBuffer, 0);

            // Read the response
            Log("Reading response of length {0}", length);
            var responseBuffer = new byte[length];
            await ReadAllAsync(stream,
                               responseBuffer,
                               responseBuffer.Length,
                               cancellationToken).ConfigureAwait(false);

            using (var reader = new BinaryReader(new MemoryStream(responseBuffer), Encoding.Unicode))
            {
                var responseType = (ResponseType)reader.ReadInt32();

                switch (responseType)
                {
                    case ResponseType.Completed:
                        return CompletedBuildResponse.Create(reader);
                    case ResponseType.MismatchedVersion:
                        return new MismatchedVersionBuildResponse();
                    case ResponseType.AnalyzerInconsistency:
                        return new AnalyzerInconsistencyBuildResponse();
                    case ResponseType.Shutdown:
                        return ShutdownBuildResponse.Create(reader);
                    case ResponseType.Rejected:
                        return new RejectedBuildResponse();
                    default:
                        throw new InvalidOperationException("Received invalid response type from server.");
                }
            }
        }
    }

    /// <summary>
    /// Represents a Response from the server. A response is as follows.
    /// 
    ///  Field Name         Type            Size (bytes)
    /// --------------------------------------------------
    ///  Length             UInteger        4
    ///  ReturnCode         Integer         4
    ///  Output             String          Variable
    ///  ErrorOutput        String          Variable
    /// 
    /// Strings are encoded via a character count prefix as a 
    /// 32-bit integer, followed by an array of characters.
    /// 
    /// </summary>
    internal sealed class CompletedBuildResponse : BuildResponse
    {
        public readonly int ReturnCode;
        public readonly bool Utf8Output;
        public readonly string Output;
        public readonly string ErrorOutput;

        public CompletedBuildResponse(int returnCode,
                                      bool utf8output,
                                      string output)
        {
            ReturnCode = returnCode;
            Utf8Output = utf8output;
            Output = output;

            // This field existed to support writing to Console.Error.  The compiler doesn't ever write to 
            // this field or Console.Error.  This field is only kept around in order to maintain the existing
            // protocol semantics.
            ErrorOutput = string.Empty;
        }

        public override ResponseType Type => ResponseType.Completed;

        public static CompletedBuildResponse Create(BinaryReader reader)
        {
            var returnCode = reader.ReadInt32();
            var utf8Output = reader.ReadBoolean();
            var output = ReadLengthPrefixedString(reader);
            var errorOutput = ReadLengthPrefixedString(reader);
            if (!string.IsNullOrEmpty(errorOutput))
            {
                throw new InvalidOperationException();
            }

            return new CompletedBuildResponse(returnCode, utf8Output, output);
        }

        protected override void AddResponseBody(BinaryWriter writer)
        {
            writer.Write(ReturnCode);
            writer.Write(Utf8Output);
            WriteLengthPrefixedString(writer, Output);
            WriteLengthPrefixedString(writer, ErrorOutput);
        }
    }

    internal sealed class ShutdownBuildResponse : BuildResponse
    {
        public readonly int ServerProcessId;

        public ShutdownBuildResponse(int serverProcessId)
        {
            ServerProcessId = serverProcessId;
        }

        public override ResponseType Type => ResponseType.Shutdown;

        protected override void AddResponseBody(BinaryWriter writer)
        {
            writer.Write(ServerProcessId);
        }

        public static ShutdownBuildResponse Create(BinaryReader reader)
        {
            var serverProcessId = reader.ReadInt32();
            return new ShutdownBuildResponse(serverProcessId);
        }
    }

    internal sealed class MismatchedVersionBuildResponse : BuildResponse
    {
        public override ResponseType Type => ResponseType.MismatchedVersion;

        /// <summary>
        /// MismatchedVersion has no body.
        /// </summary>
        protected override void AddResponseBody(BinaryWriter writer) { }
    }

    internal sealed class AnalyzerInconsistencyBuildResponse : BuildResponse
    {
        public override ResponseType Type => ResponseType.AnalyzerInconsistency;

        /// <summary>
        /// AnalyzerInconsistency has no body.
        /// </summary>
        /// <param name="writer"></param>
        protected override void AddResponseBody(BinaryWriter writer) { }
    }

    internal sealed class RejectedBuildResponse : BuildResponse
    {
        public override ResponseType Type => ResponseType.Rejected;

        /// <summary>
        /// AnalyzerInconsistency has no body.
        /// </summary>
        /// <param name="writer"></param>
        protected override void AddResponseBody(BinaryWriter writer) { }
    }

    // The id numbers below are just random. It's useful to use id numbers
    // that won't occur accidentally for debugging.
    internal enum RequestLanguage
    {
        CSharpCompile = 0x44532521,
        VisualBasicCompile = 0x44532522,
    }

    /// <summary>
    /// Constants about the protocol.
    /// </summary>
    internal static class BuildProtocolConstants
    {
        /// <summary>
        /// The version number for this protocol.
        /// </summary>
        public const uint ProtocolVersion = 2;

        // Arguments for CSharp and VB Compiler
        public enum ArgumentId
        {
            // The current directory of the client
            CurrentDirectory = 0x51147221,

            // A comment line argument. The argument index indicates which one (0 .. N)
            CommandLineArgument,

            // The "LIB" environment variable of the client
            LibEnvVariable,

            // Request a longer keep alive time for the server
            KeepAlive,

            // Request a server shutdown from the client
            Shutdown,

            // The directory to use for temporary operations.
            TempDirectory,
        }

        /// <summary>
        /// Read a string from the Reader where the string is encoded
        /// as a length prefix (signed 32-bit integer) followed by
        /// a sequence of characters.
        /// </summary>
        public static string ReadLengthPrefixedString(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            return new String(reader.ReadChars(length));
        }

        /// <summary>
        /// Write a string to the Writer where the string is encoded
        /// as a length prefix (signed 32-bit integer) follows by
        /// a sequence of characters.
        /// </summary>
        public static void WriteLengthPrefixedString(BinaryWriter writer, string value)
        {
            writer.Write(value.Length);
            writer.Write(value.ToCharArray());
        }

        /// <summary>
        /// This task does not complete until we are completely done reading.
        /// </summary>
        internal static async Task ReadAllAsync(
            Stream stream,
            byte[] buffer,
            int count,
            CancellationToken cancellationToken)
        {
            int totalBytesRead = 0;
            do
            {
                Log("Attempting to read {0} bytes from the stream",
                    count - totalBytesRead);
                int bytesRead = await stream.ReadAsync(buffer,
                                                       totalBytesRead,
                                                       count - totalBytesRead,
                                                       cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    Log("Unexpected -- read 0 bytes from the stream.");
                    throw new EndOfStreamException("Reached end of stream before end of read.");
                }
                Log("Read {0} bytes", bytesRead);
                totalBytesRead += bytesRead;
            } while (totalBytesRead < count);
            Log("Finished read");
        }
    }
}