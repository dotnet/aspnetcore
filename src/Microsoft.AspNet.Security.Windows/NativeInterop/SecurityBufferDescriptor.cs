// -----------------------------------------------------------------------
// <copyright file="SecurityBufferDescriptor.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.Windows
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe class SecurityBufferDescriptor
    {
        /*
        typedef struct _SecBufferDesc {
            ULONG        ulVersion;
            ULONG        cBuffers;
            PSecBuffer   pBuffers;
        } SecBufferDesc, * PSecBufferDesc;
        */
        public readonly int Version;
        public readonly int Count;
        public void* UnmanagedPointer;

        public SecurityBufferDescriptor(int count)
        {
            Version = 0;
            Count = count;
            UnmanagedPointer = null;
        }
    }
}
