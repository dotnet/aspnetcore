// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Combined json string and extracted byte arrays for an
    /// interop message.
    /// </summary>
    public readonly struct SerializedArgs
    {
        /// <summary>
        /// json string representing the arguments
        /// </summary>
        public readonly string? ArgsJson;

        /// <summary>
        /// Byte arrays extracted from the arguments during serialization
        /// </summary>
        public readonly byte[][]? ByteArrays;

        /// <summary>
        /// Struct containing the json args and extracted byte arrays
        /// </summary>
        /// <param name="argsJson">json string representing the arguments</param>
        /// <param name="byteArrays">Byte arrays extracted from the arguments during serialization</param>
        public SerializedArgs(string? argsJson, byte[][]? byteArrays)
        {
            ArgsJson = argsJson;
            ByteArrays = byteArrays;
        }
    }
}
