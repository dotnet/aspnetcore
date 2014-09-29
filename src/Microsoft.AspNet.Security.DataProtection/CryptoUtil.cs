// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

#if !ASPNETCORE50
using System.Runtime.ConstrainedExecution;
#endif

namespace Microsoft.AspNet.Security.DataProtection
{
    internal unsafe static class CryptoUtil
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

        // This isn't a typical Debug.Assert; the check is always performed, even in retail builds.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssertSafeHandleIsValid(SafeHandle safeHandle)
        {
            Assert(safeHandle != null && !safeHandle.IsInvalid, "Safe handle is invalid.");
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T Fail<T>(string message) where T : class
        {
            throw Fail(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
#if !ASPNETCORE50
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
#endif
        public static bool TimeConstantBuffersAreEqual(byte* bufA, byte* bufB, uint count)
        {
            bool areEqual = true;
            for (uint i = 0; i < count; i++)
            {
                areEqual &= (bufA[i] == bufB[i]);
            }
            return areEqual;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool TimeConstantBuffersAreEqual(byte[] bufA, int offsetA, int countA, byte[] bufB, int offsetB, int countB)
        {
            // Technically this is an early exit scenario, but it means that the caller did something bizarre.
            // An error at the call site isn't usable for timing attacks.
            Assert(countA == countB, "countA == countB");

            bool areEqual = true;
            for (int i = 0; i < countA; i++)
            {
                areEqual &= (bufA[offsetA + i] == bufB[offsetB + i]);
            }
            return areEqual;
        }
    }
}
