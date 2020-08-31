// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Components.Forms
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct ReadRequest
    {
        [FieldOffset(0)]
        public string InputFileElementReferenceId;

        [FieldOffset(4)]
        public int FileId;

        [FieldOffset(8)]
        public long SourceOffset;

        [FieldOffset(16)]
        public byte[] Destination;

        [FieldOffset(20)]
        public int DestinationOffset;

        [FieldOffset(24)]
        public int MaxBytes;
    }
}
