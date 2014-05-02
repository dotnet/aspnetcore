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
// <copyright file="SafeFreeContextBufferChannelBinding.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Security.Authentication.ExtendedProtection;

namespace Microsoft.AspNet.Security.Windows
{
    [SuppressUnmanagedCodeSecurity]
    internal sealed class SafeFreeContextBufferChannelBinding : ChannelBinding
    {
        private int size;

        public override int Size
        {
            get { return size; }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal unsafe void Set(IntPtr value)
        {
            this.handle = value;
        }

        internal static SafeFreeContextBufferChannelBinding CreateEmptyHandle()
        {
            return new SafeFreeContextBufferChannelBinding();
        }

        public static unsafe int QueryContextChannelBinding(SafeDeleteContext phContext, ContextAttribute contextAttribute, Bindings* buffer, 
            SafeFreeContextBufferChannelBinding refHandle)
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
                    refHandle.Set((*buffer).pBindings);
                    refHandle.size = (*buffer).BindingsLength;
                }

                if (status != 0 && refHandle != null)
                {
                    refHandle.SetHandleAsInvalid();
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
