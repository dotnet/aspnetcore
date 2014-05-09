// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Microsoft.Win32
{
    // TODO: ? [Localizable(false)]
    internal static class NativeMethods
    {
        // ReSharper disable InconsistentNaming
        public const int X509_ASN_ENCODING = 0x00000001;
        public const int X509_PUBLIC_KEY_INFO = 8;
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Encodes a structure of the type indicated by the value of the lpszStructType parameter.
        /// </summary>
        /// <param name="dwCertEncodingType">Type of encoding used.</param>
        /// <param name="lpszStructType">The high-order word is zero, the low-order word specifies the integer identifier for the type of the specified structure so
        /// we can use the constants in http://msdn.microsoft.com/en-us/library/windows/desktop/aa378145%28v=vs.85%29.aspx</param>
        /// <param name="pvStructInfo">A pointer to the structure to be encoded.</param>
        /// <param name="pbEncoded">A pointer to a buffer to receive the encoded structure. This parameter can be NULL to retrieve the size of this information for memory allocation purposes.</param>
        /// <param name="pcbEncoded">A pointer to a DWORD variable that contains the size, in bytes, of the buffer pointed to by the pbEncoded parameter.</param>
        /// <returns></returns>
        [DllImport("crypt32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptEncodeObject(
            UInt32 dwCertEncodingType,
            IntPtr lpszStructType,
            ref CERT_PUBLIC_KEY_INFO pvStructInfo,
            byte[] pbEncoded,
            ref UInt32 pcbEncoded);

        // ReSharper disable InconsistentNaming
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_BLOB
        {
            public Int32 cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CERT_CONTEXT
        {
            public Int32 dwCertEncodingType;
            public IntPtr pbCertEncoded;
            public Int32 cbCertEncoded;
            public IntPtr pCertInfo;
            public IntPtr hCertStore;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct CRYPT_ALGORITHM_IDENTIFIER
        {
            public string pszObjId;
            public CRYPT_BLOB Parameters;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct CRYPT_BIT_BLOB
        {
            public Int32 cbData;
            public IntPtr pbData;
            public Int32 cUnusedBits;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CERT_PUBLIC_KEY_INFO
        {
            public CRYPT_ALGORITHM_IDENTIFIER Algorithm;
            public CRYPT_BIT_BLOB PublicKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class CERT_INFO
        {
            public Int32 dwVersion;
            public CRYPT_BLOB SerialNumber;
            public CRYPT_ALGORITHM_IDENTIFIER SignatureAlgorithm;
            public CRYPT_BLOB Issuer;
            public System.Runtime.InteropServices.ComTypes.FILETIME NotBefore;
            public System.Runtime.InteropServices.ComTypes.FILETIME NotAfter;
            public CRYPT_BLOB Subject;
            public CERT_PUBLIC_KEY_INFO SubjectPublicKeyInfo;
            public CRYPT_BIT_BLOB IssuerUniqueId;
            public CRYPT_BIT_BLOB SubjectUniqueId;
            public Int32 cExtension;
            public IntPtr rgExtension;
        }

        // ReSharper restore InconsistentNaming
    }
}
