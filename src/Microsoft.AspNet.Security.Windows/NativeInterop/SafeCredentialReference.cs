// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
