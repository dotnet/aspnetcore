using System;
using System.Runtime.InteropServices;

#if !NET45
namespace Microsoft.Win32.SafeHandles {
    internal abstract class SafeHandleZeroOrMinusOneIsInvalid : SafeHandle {
        // Called by P/Invoke when returning SafeHandles
        protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle) {
        }

        public override bool IsInvalid {
            get {
                return (handle == IntPtr.Zero || handle == (IntPtr)(-1));
            }
        }
    }
}
#endif