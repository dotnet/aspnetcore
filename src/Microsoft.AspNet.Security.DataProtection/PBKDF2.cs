using System;
using System.Security.Cryptography;

namespace Microsoft.AspNet.Security.DataProtection
{
    /// <summary>
    /// Helper class to derive keys from low-entropy passwords using the PBKDF2 algorithm.
    /// </summary>
    public static class PBKDF2
    {
        /// <summary>
        /// Derives a key from a low-entropy password.
        /// </summary>
        /// <param name="algorithmName">The name of the PRF to use for key derivation.</param>
        /// <param name="password">The low-entropy password from which to generate a key.</param>
        /// <param name="salt">The salt used to randomize the key derivation.</param>
        /// <param name="iterationCount">The number of iterations to perform.</param>
        /// <param name="numBytesToDerive">The desired byte length of the derived key.</param>
        /// <returns>A key derived from the provided password.</returns>
        /// <remarks>For compatibility with the Rfc2898DeriveBytes class, specify "SHA1" for the <em>algorithmName</em> parameter.</remarks>
        public unsafe static byte[] DeriveKey(string algorithmName, byte[] password, byte[] salt, ulong iterationCount, uint numBytesToDerive)
        {
            if (String.IsNullOrEmpty(algorithmName))
            {
                throw new ArgumentException(Res.Common_NullOrEmpty, "algorithmName");
            }
            if (password == null || password.Length == 0)
            {
                throw new ArgumentException(Res.Common_NullOrEmpty, "password");
            }
            if (salt == null || salt.Length == 0)
            {
                throw new ArgumentException(Res.Common_NullOrEmpty, "salt");
            }
            if (iterationCount <= 0)
            {
                throw new ArgumentOutOfRangeException("iterationCount");
            }

            byte[] derivedKey = new byte[numBytesToDerive];
            int status;

            using (BCryptAlgorithmHandle algHandle = Algorithms.CreateGenericHMACHandleFromPrimitiveProvider(algorithmName))
            {
                fixed (byte* pPassword = password)
                fixed (byte* pSalt = salt)
                fixed (byte* pDerivedKey = derivedKey)
                {
                    status = UnsafeNativeMethods.BCryptDeriveKeyPBKDF2(
                        algHandle, pPassword, (uint)password.Length, pSalt, (uint)salt.Length, iterationCount,
                        pDerivedKey, numBytesToDerive, dwFlags: 0);
                }
            }

            if (status == 0 /* STATUS_SUCCESS */)
            {
                return derivedKey;
            }
            else
            {
                throw new CryptographicException(status);
            }
        }
    }
}
