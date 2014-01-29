using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.DataProtection
{
    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375524(v=vs.85).aspx
    [StructLayout(LayoutKind.Sequential)]
    internal struct BCRYPT_KEY_DATA_BLOB_HEADER
    {
        // from bcrypt.h
        private const uint BCRYPT_KEY_DATA_BLOB_MAGIC = 0x4d42444b; //Key Data Blob Magic (KDBM)
        private const uint BCRYPT_KEY_DATA_BLOB_VERSION1 = 0x1;

        public uint dwMagic;
        public uint dwVersion;
        public uint cbKeyData;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Initialize(ref BCRYPT_KEY_DATA_BLOB_HEADER pHeader)
        {
            pHeader.dwMagic = BCRYPT_KEY_DATA_BLOB_MAGIC;
            pHeader.dwVersion = BCRYPT_KEY_DATA_BLOB_VERSION1;
        }
    }
}