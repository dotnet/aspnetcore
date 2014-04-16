using System;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNet.Security.DataProtection
{
    internal sealed class BCryptHashHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Called by P/Invoke when returning SafeHandles
        private BCryptHashHandle()
            : base(ownsHandle: true)
        {
        }

        // Do not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle for you.
        protected override bool ReleaseHandle()
        {
            return (UnsafeNativeMethods.BCryptDestroyHash(handle) == 0);
        }
    }
}
