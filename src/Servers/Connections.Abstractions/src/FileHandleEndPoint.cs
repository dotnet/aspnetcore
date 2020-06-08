// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;

namespace Microsoft.AspNetCore.Connections
{
    public class FileHandleEndPoint : EndPoint
    {
        public FileHandleEndPoint(ulong fileHandle, FileHandleType fileHandleType) :
            this(fileHandle, fileHandleType, ownsHandle: true)
        {
        }

        public FileHandleEndPoint(ulong fileHandle, FileHandleType fileHandleType, bool ownsHandle)
        {
            FileHandle = fileHandle;
            FileHandleType = fileHandleType;
            OwnsHandle = ownsHandle;

            switch (fileHandleType)
            {
                case FileHandleType.Auto:
                case FileHandleType.Tcp:
                case FileHandleType.Pipe:
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public bool OwnsHandle { get; private set; }
        public ulong FileHandle { get; }
        public FileHandleType FileHandleType { get; }
    }
}
