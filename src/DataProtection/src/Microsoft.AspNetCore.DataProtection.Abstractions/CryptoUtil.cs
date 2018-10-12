// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.DataProtection
{
    internal static class CryptoUtil
    {
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

        // Allows callers to write "var x = Method() ?? Fail<T>(message);" as a convenience to guard
        // against a method returning null unexpectedly.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T Fail<T>(string message) where T : class
        {
            throw Fail(message);
        }
    }
}
