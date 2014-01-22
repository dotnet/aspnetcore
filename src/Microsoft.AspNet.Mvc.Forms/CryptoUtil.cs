// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;

namespace Microsoft.AspNet.Mvc
{
    internal static class CryptoUtil
    {
        // This method is specially written to take the same amount of time
        // regardless of where 'a' and 'b' differ. Please do not optimize it.
        public static bool AreByteArraysEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            bool areEqual = true;
            for (int i = 0; i < a.Length; i++)
            {
                areEqual &= (a[i] == b[i]);
            }
            return areEqual;
        }

        // Computes a SHA256 hash over all of the input parameters.
        // Each parameter is UTF8 encoded and preceded by a 7-bit encoded
        // integer describing the encoded byte length of the string.
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "MemoryStream is resilient to double-Dispose")]
        public static byte[] ComputeSHA256(IList<string> parameters)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    foreach (string parameter in parameters)
                    {
                        bw.Write(parameter); // also writes the length as a prefix; unambiguous
                    }
                    bw.Flush();

                    using (SHA256Cng sha256 = new SHA256Cng())
                    {
                        byte[] retVal = sha256.ComputeHash(ms.GetBuffer(), 0, checked((int)ms.Length));
                        return retVal;
                    }
                }
            }
        }
    }
}
