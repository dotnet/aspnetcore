// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Components.Forms
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct ReadRequest
    {
        // Even though this type is only intended for use on WebAssembly, make it able to
        // load on 64-bit runtimes by allowing 8 bytes for each reference-typed field.

        [FieldOffset(0)]
        public string InputFileElementReferenceId;

        [FieldOffset(8)]
        public int FileId;

        [FieldOffset(12)]
        public long SourceOffset;

        [FieldOffset(24)]
        public byte[] Destination;

        [FieldOffset(32)]
        public int DestinationOffset;

        [FieldOffset(36)]
        public int MaxBytes;
    }
}
