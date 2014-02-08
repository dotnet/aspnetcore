// -----------------------------------------------------------------------
// <copyright file="SafeCredentialReference.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNet.Security.Windows
{
    internal sealed class SafeCredentialReference : CriticalHandleMinusOneIsInvalid
    {
        // Static cache will return the target handle if found the reference in the table.
        internal SafeFreeCredentials _Target;

        private SafeCredentialReference(SafeFreeCredentials target)
            : base()
        {
            // Bumps up the refcount on Target to signify that target handle is statically cached so
            // its dispose should be postponed
            bool b = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                target.DangerousAddRef(ref b);
            }
            catch
            {
                if (b)
                {
                    target.DangerousRelease();
                    b = false;
                }
            }
            finally
            {
                if (b)
                {
                    _Target = target;
                    SetHandle(new IntPtr(0));   // make this handle valid
                }
            }
        }

        internal static SafeCredentialReference CreateReference(SafeFreeCredentials target)
        {
            SafeCredentialReference result = new SafeCredentialReference(target);
            if (result.IsInvalid)
            {
                return null;
            }

            return result;
        }

        protected override bool ReleaseHandle()
        {
            SafeFreeCredentials target = _Target;
            if (target != null)
            {
                target.DangerousRelease();
            }
            _Target = null;
            return true;
        }
    }
}
