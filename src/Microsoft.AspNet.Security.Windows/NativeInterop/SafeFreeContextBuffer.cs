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
// <copyright file="SafeFreeContextBuffer.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNet.Security.Windows
{
    [SuppressUnmanagedCodeSecurity]
    internal sealed class SafeFreeContextBuffer : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeFreeContextBuffer()
            : base(true)
        {
        }

        // This must be ONLY called from this file and in the context of a CER
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal unsafe void Set(IntPtr value)
        {
            this.handle = value;
        }

        internal static int EnumeratePackages(out int pkgnum, out SafeFreeContextBuffer pkgArray)
        {
            int res = -1;
            res = UnsafeNclNativeMethods.SafeNetHandles.EnumerateSecurityPackagesW(out pkgnum, out pkgArray);
            if (res != 0 && pkgArray != null)
            {
                pkgArray.SetHandleAsInvalid();
            }
            return res;
        }

        internal static SafeFreeContextBuffer CreateEmptyHandle()
        {
            return new SafeFreeContextBuffer();
        }

        // After PINvoke call the method will fix the refHandle.handle with the returned value.
        // The caller is responsible for creating a correct SafeHandle template or null can be passed if no handle is returned.
        //
        // This method switches between three non-interruptible helper methods.  (This method can't be both non-interruptible and
        // reference imports from all three DLLs - doing so would cause all three DLLs to try to be bound to.)

        public static unsafe int QueryContextAttributes(SafeDeleteContext phContext, ContextAttribute contextAttribute, byte* buffer, SafeHandle refHandle)
        {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false;

            // We don't want to be interrupted by thread abort exceptions or unexpected out-of-memory errors failing to jit
            // one of the following methods. So run within a CER non-interruptible block.
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                phContext.DangerousAddRef(ref b);
            }
            catch (Exception e)
            {
                if (b)
                {
                    phContext.DangerousRelease();
                    b = false;
                }
                if (!(e is ObjectDisposedException))
                {
                    throw;
                }
            }
            finally
            {
                if (b)
                {
                    status = UnsafeNclNativeMethods.SafeNetHandles.QueryContextAttributesW(ref phContext._handle, contextAttribute, buffer);
                    phContext.DangerousRelease();
                }

                if (status == 0 && refHandle != null)
                {
                    if (refHandle is SafeFreeContextBuffer)
                    {
                        ((SafeFreeContextBuffer)refHandle).Set(*(IntPtr*)buffer);
                    }
                    else
                    {
                        ((SafeFreeCertContext)refHandle).Set(*(IntPtr*)buffer);
                    }
                }

                if (status != 0 && refHandle != null)
                {
                    refHandle.SetHandleAsInvalid();
                }
            }

            return status;
        }

        public static int SetContextAttributes(SafeDeleteContext phContext, ContextAttribute contextAttribute, byte[] buffer)
        {
            int status = (int)SecurityStatus.InvalidHandle;
            bool b = false;

            // We don't want to be interrupted by thread abort exceptions or unexpected out-of-memory errors failing 
            // to jit one of the following methods. So run within a CER non-interruptible block.
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                phContext.DangerousAddRef(ref b);
            }
            catch (Exception e)
            {
                if (b)
                {
                    phContext.DangerousRelease();
                    b = false;
                }
                if (!(e is ObjectDisposedException))
                {
                    throw;
                }
            }
            finally
            {
                if (b)
                {
                    status = UnsafeNclNativeMethods.SafeNetHandles.SetContextAttributesW(
                        ref phContext._handle, contextAttribute, buffer, buffer.Length);
                    phContext.DangerousRelease();
                }
            }

            return status;
        }

        protected override bool ReleaseHandle()
        {
            return UnsafeNclNativeMethods.SafeNetHandles.FreeContextBuffer(handle) == 0;
        }
    }
}
