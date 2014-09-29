// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal static class CryptoUtil
    {
        // This isn't a typical Debug.Assert; the check is always performed, even in retail builds.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                Fail(message);
            }
        }

        // This isn't a typical Debug.Fail; an error always occurs, even in retail builds.
        // This method doesn't return, but since the CLR doesn't allow specifying a 'never'
        // return type, we mimic it by specifying our return type as Exception. That way
        // callers can write 'throw Fail(...);' to make the C# compiler happy, as the
        // throw keyword is implicitly of type O.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Exception Fail(string message)
        {
            Debug.Fail(message);
            throw new CryptographicException("Assertion failed: " + message);
        }
    }
}
