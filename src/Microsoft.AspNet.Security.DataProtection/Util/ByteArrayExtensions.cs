// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNet.Security.DataProtection.Util
{
    /// <summary>
    /// Defines helper methods for working with fixed expression blocks.
    /// </summary>
    internal static class ByteArrayExtensions
    {
        private static readonly byte[] _dummyBuffer = new byte[1];

        // Since the 'fixed' keyword turns a zero-length array into a pointer, we need
        // to make sure we're always providing a buffer of length >= 1 so that the
        // p/invoke methods we pass the pointers to don't see a null pointer. Callers
        // are still responsible for passing a proper length to the p/invoke routines.
        public static byte[] AsFixed(this byte[] buffer)
        {
            Debug.Assert(buffer != null);
            return (buffer.Length != 0) ? buffer : _dummyBuffer;
        }
    }
}
