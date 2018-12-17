// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Tools
{
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
    internal sealed class CompletedServerResponse : ServerResponse
    {
        public readonly int ReturnCode;
        public readonly bool Utf8Output;
        public readonly string Output;
        public readonly string ErrorOutput;

        public CompletedServerResponse(int returnCode, bool utf8output, string output, string error)
        {
            ReturnCode = returnCode;
            Utf8Output = utf8output;
            Output = output;
            ErrorOutput = error;
        }

        public override ResponseType Type => ResponseType.Completed;

        public static CompletedServerResponse Create(BinaryReader reader)
        {
            var returnCode = reader.ReadInt32();
            var utf8Output = reader.ReadBoolean();
            var output = ServerProtocol.ReadLengthPrefixedString(reader);
            var errorOutput = ServerProtocol.ReadLengthPrefixedString(reader);

            return new CompletedServerResponse(returnCode, utf8Output, output, errorOutput);
        }

        protected override void AddResponseBody(BinaryWriter writer)
        {
            writer.Write(ReturnCode);
            writer.Write(Utf8Output);
            ServerProtocol.WriteLengthPrefixedString(writer, Output);
            ServerProtocol.WriteLengthPrefixedString(writer, ErrorOutput);
        }
    }
}
