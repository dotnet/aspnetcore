// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Razor.Tools
{
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
    internal readonly struct RequestArgument
    {
        public readonly ArgumentId Id;
        public readonly int ArgumentIndex;
        public readonly string Value;

        public RequestArgument(ArgumentId argumentId, int argumentIndex, string value)
        {
            Id = argumentId;
            ArgumentIndex = argumentIndex;
            Value = value;
        }

        public static RequestArgument ReadFromBinaryReader(BinaryReader reader)
        {
            var argId = (ArgumentId)reader.ReadInt32();
            var argIndex = reader.ReadInt32();
            var value = ServerProtocol.ReadLengthPrefixedString(reader);
            return new RequestArgument(argId, argIndex, value);
        }

        public void WriteToBinaryWriter(BinaryWriter writer)
        {
            writer.Write((int)Id);
            writer.Write(ArgumentIndex);
            ServerProtocol.WriteLengthPrefixedString(writer, Value);
        }

        public enum ArgumentId
        {
            // The current directory of the client
            CurrentDirectory = 0x51147221,

            // A comment line argument. The argument index indicates which one (0 .. N)
            CommandLineArgument,

            // Request a longer keep alive time for the server
            KeepAlive,

            // Request a server shutdown from the client
            Shutdown,

            // The directory to use for temporary operations.
            TempDirectory,
        }

        public override string ToString()
        {
            return $"{Id} {Value}";
        }
    }
}
