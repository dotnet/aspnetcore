using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.DataProtection
{
    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa375370(v=vs.85).aspx
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct BCryptBufferDesc
    {
        private const int BCRYPTBUFFER_VERSION = 0;

        public uint ulVersion; // Version number
        public uint cBuffers; // Number of buffers
        public BCryptBuffer* pBuffers; // Pointer to array of buffers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Initialize(ref BCryptBufferDesc bufferDesc)
        {
            bufferDesc.ulVersion = BCRYPTBUFFER_VERSION;
        }
    }
}
