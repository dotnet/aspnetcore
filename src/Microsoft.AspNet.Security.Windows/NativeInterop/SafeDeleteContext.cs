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
// <copyright file="SafeDeleteContext.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Security.Windows
{
    internal sealed class SafeDeleteContext : SafeHandle
    {
        private const string DummyStr = " ";
        private static readonly byte[] DummyBytes = new byte[] { 0 };

        // ATN: _handle is internal since it is used on PInvokes by other wrapper methods.
        //      However all such wrappers MUST manually and reliably adjust refCounter of SafeDeleteContext handle.

        internal SSPIHandle _handle;

        private SafeFreeCredentials _effectiveCredential;

        private SafeDeleteContext()
            : base(IntPtr.Zero, true)
        {
            _handle = new SSPIHandle();
        }

        public override bool IsInvalid
        {
            get
            {
                return IsClosed || _handle.IsZero;
            }
        }

        public override string ToString()
        {
            return _handle.ToString();
        }

        //-------------------------------------------------------------------
        internal static unsafe int InitializeSecurityContext(ref SafeFreeCredentials inCredentials, ref SafeDeleteContext refContext, 
            string targetName, ContextFlags inFlags, Endianness endianness, SecurityBuffer inSecBuffer, SecurityBuffer[] inSecBuffers, 
            SecurityBuffer outSecBuffer, ref ContextFlags outFlags)
        {
            GlobalLog.Assert(outSecBuffer != null, "SafeDeleteContext::InitializeSecurityContext()|outSecBuffer != null");
            GlobalLog.Assert(inSecBuffer == null || inSecBuffers == null, "SafeDeleteContext::InitializeSecurityContext()|inSecBuffer == null || inSecBuffers == null");

            if (inCredentials == null)
            {
                throw new ArgumentNullException("inCredentials");
            }

            SecurityBufferDescriptor inSecurityBufferDescriptor = null;
            if (inSecBuffer != null)
            {
                inSecurityBufferDescriptor = new SecurityBufferDescriptor(1);
            }
            else if (inSecBuffers != null)
            {
                inSecurityBufferDescriptor = new SecurityBufferDescriptor(inSecBuffers.Length);
            }
            SecurityBufferDescriptor outSecurityBufferDescriptor = new SecurityBufferDescriptor(1);

            // actually this is returned in outFlags
            bool isSspiAllocated = (inFlags & ContextFlags.AllocateMemory) != 0 ? true : false;

            int errorCode = -1;

            SSPIHandle contextHandle = new SSPIHandle();
            if (refContext != null)
            {
                contextHandle = refContext._handle;
            }

            // these are pinned user byte arrays passed along with SecurityBuffers
            GCHandle[] pinnedInBytes = null;
            GCHandle pinnedOutBytes = new GCHandle();
            // optional output buffer that may need to be freed
            SafeFreeContextBuffer outFreeContextBuffer = null;
            try
            {
                pinnedOutBytes = GCHandle.Alloc(outSecBuffer.token, GCHandleType.Pinned);
                SecurityBufferStruct[] inUnmanagedBuffer = new SecurityBufferStruct[inSecurityBufferDescriptor == null ? 1 : inSecurityBufferDescriptor.Count];
                fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
                {
                    if (inSecurityBufferDescriptor != null)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers
                        inSecurityBufferDescriptor.UnmanagedPointer = inUnmanagedBufferPtr;
                        pinnedInBytes = new GCHandle[inSecurityBufferDescriptor.Count];
                        SecurityBuffer securityBuffer;
                        for (int index = 0; index < inSecurityBufferDescriptor.Count; ++index)
                        {
                            securityBuffer = inSecBuffer != null ? inSecBuffer : inSecBuffers[index];
                            if (securityBuffer != null)
                            {
                                // Copy the SecurityBuffer content into unmanaged place holder
                                inUnmanagedBuffer[index].count = securityBuffer.size;
                                inUnmanagedBuffer[index].type = securityBuffer.type;

                                // use the unmanaged token if it's not null; otherwise use the managed buffer
                                if (securityBuffer.unmanagedToken != null)
                                {
                                    inUnmanagedBuffer[index].token = securityBuffer.unmanagedToken.DangerousGetHandle();
                                }
                                else if (securityBuffer.token == null || securityBuffer.token.Length == 0)
                                {
                                    inUnmanagedBuffer[index].token = IntPtr.Zero;
                                }
                                else
                                {
                                    pinnedInBytes[index] = GCHandle.Alloc(securityBuffer.token, GCHandleType.Pinned);
                                    inUnmanagedBuffer[index].token = Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
                                }
                            }
                        }
                    }

                    SecurityBufferStruct[] outUnmanagedBuffer = new SecurityBufferStruct[1];
                    fixed (void* outUnmanagedBufferPtr = outUnmanagedBuffer)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers
                        outSecurityBufferDescriptor.UnmanagedPointer = outUnmanagedBufferPtr;
                        outUnmanagedBuffer[0].count = outSecBuffer.size;
                        outUnmanagedBuffer[0].type = outSecBuffer.type;
                        if (outSecBuffer.token == null || outSecBuffer.token.Length == 0)
                        {
                            outUnmanagedBuffer[0].token = IntPtr.Zero;
                        }
                        else
                        {
                            outUnmanagedBuffer[0].token = Marshal.UnsafeAddrOfPinnedArrayElement(outSecBuffer.token, outSecBuffer.offset);
                        }
                        if (isSspiAllocated)
                        {
                            outFreeContextBuffer = SafeFreeContextBuffer.CreateEmptyHandle();
                        }

                        if (refContext == null || refContext.IsInvalid)
                        {
                            refContext = new SafeDeleteContext();
                        }

                        if (targetName == null || targetName.Length == 0)
                        {
                            targetName = DummyStr;
                        }

                        fixed (char* namePtr = targetName)
                        {
                            errorCode = MustRunInitializeSecurityContext(
                                            ref inCredentials,
                                            contextHandle.IsZero ? null : &contextHandle,
                                            (byte*)(((object)targetName == (object)DummyStr) ? null : namePtr),
                                            inFlags,
                                            endianness,
                                            inSecurityBufferDescriptor,
                                            refContext,
                                            outSecurityBufferDescriptor,
                                            ref outFlags,
                                            outFreeContextBuffer);
                        }

                        GlobalLog.Print("SafeDeleteContext:InitializeSecurityContext  Marshalling OUT buffer");
                        // Get unmanaged buffer with index 0 as the only one passed into PInvoke
                        outSecBuffer.size = outUnmanagedBuffer[0].count;
                        outSecBuffer.type = outUnmanagedBuffer[0].type;
                        if (outSecBuffer.size > 0)
                        {
                            outSecBuffer.token = new byte[outSecBuffer.size];
                            Marshal.Copy(outUnmanagedBuffer[0].token, outSecBuffer.token, 0, outSecBuffer.size);
                        }
                        else
                        {
                            outSecBuffer.token = null;
                        }
                    }
                }
            }
            finally
            {
                if (pinnedInBytes != null)
                {
                    for (int index = 0; index < pinnedInBytes.Length; index++)
                    {
                        if (pinnedInBytes[index].IsAllocated)
                        {
                            pinnedInBytes[index].Free();
                        }
                    }
                }
                if (pinnedOutBytes.IsAllocated)
                {
                    pinnedOutBytes.Free();
                }

                if (outFreeContextBuffer != null)
                {
                    outFreeContextBuffer.Dispose();
                }
            }

            GlobalLog.Leave("SafeDeleteContext::InitializeSecurityContext() unmanaged InitializeSecurityContext()", "errorCode:0x" + errorCode.ToString("x8") + " refContext:" + ValidationHelper.ToString(refContext));

            return errorCode;
        }

        // After PINvoke call the method will fix the handleTemplate.handle with the returned value.
        // The caller is responsible for creating a correct SafeFreeContextBuffer_XXX flavour or null can be passed if no handle is returned.
        //
        // Since it has a CER, this method can't have any references to imports from DLLs that may not exist on the system.

        private static unsafe int MustRunInitializeSecurityContext(
                                                  ref SafeFreeCredentials inCredentials,
                                                  void* inContextPtr,
                                                  byte* targetName,
                                                  ContextFlags inFlags,
                                                  Endianness endianness,
                                                  SecurityBufferDescriptor inputBuffer,
                                                  SafeDeleteContext outContext,
                                                  SecurityBufferDescriptor outputBuffer,
                                                  ref ContextFlags attributes,
                                                  SafeFreeContextBuffer handleTemplate)
        {
            int errorCode = (int)SecurityStatus.InvalidHandle;
            bool b1 = false;
            bool b2 = false;

            // Run the body of this method as a non-interruptible block.
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                inCredentials.DangerousAddRef(ref b1);
                outContext.DangerousAddRef(ref b2);
            }
            catch (Exception e)
            {
                if (b1)
                {
                    inCredentials.DangerousRelease();
                    b1 = false;
                }
                if (b2)
                {
                    outContext.DangerousRelease();
                    b2 = false;
                }

                if (!(e is ObjectDisposedException))
                {
                    throw;
                }
            }
            finally
            {
                SSPIHandle credentialHandle = inCredentials._handle;
                long timeStamp;

                if (!b1)
                {
                    // caller should retry
                    inCredentials = null;
                }
                else if (b1 && b2)
                {
                    errorCode = UnsafeNclNativeMethods.SafeNetHandles.InitializeSecurityContextW(
                                ref credentialHandle,
                                inContextPtr,
                                targetName,
                                inFlags,
                                0,
                                endianness,
                                inputBuffer,
                                0,
                                ref outContext._handle,
                                outputBuffer,
                                ref attributes,
                                out timeStamp);

                    // When a credential handle is first associated with the context we keep credential
                    // ref count bumped up to ensure ordered finalization.
                    // If the credential handle has been changed we de-ref the old one and associate the
                    //  context with the new cred handle but only if the call was successful.
                    if (outContext._effectiveCredential != inCredentials && (errorCode & 0x80000000) == 0)
                    {
                        // Disassociate the previous credential handle
                        if (outContext._effectiveCredential != null)
                        {
                            outContext._effectiveCredential.DangerousRelease();
                        }
                        outContext._effectiveCredential = inCredentials;
                    }
                    else
                    {
                        inCredentials.DangerousRelease();
                    }

                    outContext.DangerousRelease();

                    // The idea is that SSPI has allocated a block and filled up outUnmanagedBuffer+8 slot with the pointer.
                    if (handleTemplate != null)
                    {
                        handleTemplate.Set(((SecurityBufferStruct*)outputBuffer.UnmanagedPointer)->token); // ATTN: on 64 BIT that is still +8 cause of 2* c++ unsigned long == 8 bytes
                        if (handleTemplate.IsInvalid)
                        {
                            handleTemplate.SetHandleAsInvalid();
                        }
                    }
                }

                if (inContextPtr == null && (errorCode & 0x80000000) != 0)
                {
                    // an error on the first call, need to set the out handle to invalid value
                    outContext._handle.SetToInvalid();
                }
            }

            return errorCode;
        }

        //-------------------------------------------------------------------
        internal static unsafe int AcceptSecurityContext(ref SafeFreeCredentials inCredentials, ref SafeDeleteContext refContext, 
            ContextFlags inFlags, Endianness endianness, SecurityBuffer inSecBuffer, SecurityBuffer[] inSecBuffers, SecurityBuffer outSecBuffer, 
            ref ContextFlags outFlags)
        {
            GlobalLog.Assert(outSecBuffer != null, "SafeDeleteContext::AcceptSecurityContext()|outSecBuffer != null");
            GlobalLog.Assert(inSecBuffer == null || inSecBuffers == null, "SafeDeleteContext::AcceptSecurityContext()|inSecBuffer == null || inSecBuffers == null");

            if (inCredentials == null)
            {
                throw new ArgumentNullException("inCredentials");
            }

            SecurityBufferDescriptor inSecurityBufferDescriptor = null;
            if (inSecBuffer != null)
            {
                inSecurityBufferDescriptor = new SecurityBufferDescriptor(1);
            }
            else if (inSecBuffers != null)
            {
                inSecurityBufferDescriptor = new SecurityBufferDescriptor(inSecBuffers.Length);
            }
            SecurityBufferDescriptor outSecurityBufferDescriptor = new SecurityBufferDescriptor(1);

            // actually this is returned in outFlags
            bool isSspiAllocated = (inFlags & ContextFlags.AllocateMemory) != 0 ? true : false;

            int errorCode = -1;

            SSPIHandle contextHandle = new SSPIHandle();
            if (refContext != null)
            {
                contextHandle = refContext._handle;
            }

            // these are pinned user byte arrays passed along with SecurityBuffers
            GCHandle[] pinnedInBytes = null;
            GCHandle pinnedOutBytes = new GCHandle();
            // optional output buffer that may need to be freed
            SafeFreeContextBuffer outFreeContextBuffer = null;
            try
            {
                pinnedOutBytes = GCHandle.Alloc(outSecBuffer.token, GCHandleType.Pinned);
                SecurityBufferStruct[] inUnmanagedBuffer = new SecurityBufferStruct[inSecurityBufferDescriptor == null ? 1 : inSecurityBufferDescriptor.Count];
                fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
                {
                    if (inSecurityBufferDescriptor != null)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers
                        inSecurityBufferDescriptor.UnmanagedPointer = inUnmanagedBufferPtr;
                        pinnedInBytes = new GCHandle[inSecurityBufferDescriptor.Count];
                        SecurityBuffer securityBuffer;
                        for (int index = 0; index < inSecurityBufferDescriptor.Count; ++index)
                        {
                            securityBuffer = inSecBuffer != null ? inSecBuffer : inSecBuffers[index];
                            if (securityBuffer != null)
                            {
                                // Copy the SecurityBuffer content into unmanaged place holder
                                inUnmanagedBuffer[index].count = securityBuffer.size;
                                inUnmanagedBuffer[index].type = securityBuffer.type;

                                // use the unmanaged token if it's not null; otherwise use the managed buffer
                                if (securityBuffer.unmanagedToken != null)
                                {
                                    inUnmanagedBuffer[index].token = securityBuffer.unmanagedToken.DangerousGetHandle();
                                }
                                else if (securityBuffer.token == null || securityBuffer.token.Length == 0)
                                {
                                    inUnmanagedBuffer[index].token = IntPtr.Zero;
                                }
                                else
                                {
                                    pinnedInBytes[index] = GCHandle.Alloc(securityBuffer.token, GCHandleType.Pinned);
                                    inUnmanagedBuffer[index].token = Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
                                }
                            }
                        }
                    }
                    SecurityBufferStruct[] outUnmanagedBuffer = new SecurityBufferStruct[1];
                    fixed (void* outUnmanagedBufferPtr = outUnmanagedBuffer)
                    {
                        // Fix Descriptor pointer that points to unmanaged SecurityBuffers
                        outSecurityBufferDescriptor.UnmanagedPointer = outUnmanagedBufferPtr;
                        // Copy the SecurityBuffer content into unmanaged place holder
                        outUnmanagedBuffer[0].count = outSecBuffer.size;
                        outUnmanagedBuffer[0].type = outSecBuffer.type;

                        if (outSecBuffer.token == null || outSecBuffer.token.Length == 0)
                        {
                            outUnmanagedBuffer[0].token = IntPtr.Zero;
                        }
                        else
                        {
                            outUnmanagedBuffer[0].token = Marshal.UnsafeAddrOfPinnedArrayElement(outSecBuffer.token, outSecBuffer.offset);
                        }
                        if (isSspiAllocated)
                        {
                            outFreeContextBuffer = SafeFreeContextBuffer.CreateEmptyHandle();
                        }

                        if (refContext == null || refContext.IsInvalid)
                        {
                            refContext = new SafeDeleteContext();
                        }

                        errorCode = MustRunAcceptSecurityContext(
                                        ref inCredentials,
                                        contextHandle.IsZero ? null : &contextHandle,
                                        inSecurityBufferDescriptor,
                                        inFlags,
                                        endianness,
                                        refContext,
                                        outSecurityBufferDescriptor,
                                        ref outFlags,
                                        outFreeContextBuffer);

                        GlobalLog.Print("SafeDeleteContext:AcceptSecurityContext  Marshalling OUT buffer");
                        // Get unmanaged buffer with index 0 as the only one passed into PInvoke
                        outSecBuffer.size = outUnmanagedBuffer[0].count;
                        outSecBuffer.type = outUnmanagedBuffer[0].type;
                        if (outSecBuffer.size > 0)
                        {
                            outSecBuffer.token = new byte[outSecBuffer.size];
                            Marshal.Copy(outUnmanagedBuffer[0].token, outSecBuffer.token, 0, outSecBuffer.size);
                        }
                        else
                        {
                            outSecBuffer.token = null;
                        }
                    }
                }
            }
            finally
            {
                if (pinnedInBytes != null)
                {
                    for (int index = 0; index < pinnedInBytes.Length; index++)
                    {
                        if (pinnedInBytes[index].IsAllocated)
                        {
                            pinnedInBytes[index].Free();
                        }
                    }
                }

                if (pinnedOutBytes.IsAllocated)
                {
                    pinnedOutBytes.Free();
                }

                if (outFreeContextBuffer != null)
                {
                    outFreeContextBuffer.Dispose();
                }
            }

            GlobalLog.Leave("SafeDeleteContext::AcceptSecurityContex() unmanaged AcceptSecurityContex()", "errorCode:0x" + errorCode.ToString("x8") + " refContext:" + ValidationHelper.ToString(refContext));

            return errorCode;
        }

        // After PINvoke call the method will fix the handleTemplate.handle with the returned value.
        // The caller is responsible for creating a correct SafeFreeContextBuffer_XXX flavour or null can be passed if no handle is returned.
        //
        // Since it has a CER, this method can't have any references to imports from DLLs that may not exist on the system.

        private static unsafe int MustRunAcceptSecurityContext(
                                                  ref SafeFreeCredentials inCredentials,
                                                  void* inContextPtr,
                                                  SecurityBufferDescriptor inputBuffer,
                                                  ContextFlags inFlags,
                                                  Endianness endianness,
                                                  SafeDeleteContext outContext,
                                                  SecurityBufferDescriptor outputBuffer,
                                                  ref ContextFlags outFlags,
                                                  SafeFreeContextBuffer handleTemplate)
        {
            int errorCode = (int)SecurityStatus.InvalidHandle;
            bool b1 = false;
            bool b2 = false;

            // Run the body of this method as a non-interruptible block.
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                inCredentials.DangerousAddRef(ref b1);
                outContext.DangerousAddRef(ref b2);
            }
            catch (Exception e)
            {
                if (b1)
                {
                    inCredentials.DangerousRelease();
                    b1 = false;
                }
                if (b2)
                {
                    outContext.DangerousRelease();
                    b2 = false;
                }
                if (!(e is ObjectDisposedException))
                {
                    throw;
                }
            }
            finally
            {
                SSPIHandle credentialHandle = inCredentials._handle;
                long timeStamp;

                if (!b1)
                {
                    // caller should retry
                    inCredentials = null;
                }
                else if (b1 && b2)
                {
                    errorCode = UnsafeNclNativeMethods.SafeNetHandles.AcceptSecurityContext(
                                ref credentialHandle,
                                inContextPtr,
                                inputBuffer,
                                inFlags,
                                endianness,
                                ref outContext._handle,
                                outputBuffer,
                                ref outFlags,
                                out timeStamp);

                    // When a credential handle is first associated with the context we keep credential
                    // ref count bumped up to ensure ordered finalization.
                    // If the credential handle has been changed we de-ref the old one and associate the
                    //  context with the new cred handle but only if the call was successful.
                    if (outContext._effectiveCredential != inCredentials && (errorCode & 0x80000000) == 0)
                    {
                        // Disassociate the previous credential handle
                        if (outContext._effectiveCredential != null)
                        {
                            outContext._effectiveCredential.DangerousRelease();
                        }
                        outContext._effectiveCredential = inCredentials;
                    }
                    else
                    {
                        inCredentials.DangerousRelease();
                    }

                    outContext.DangerousRelease();

                    // The idea is that SSPI has allocated a block and filled up outUnmanagedBuffer+8 slot with the pointer.
                    if (handleTemplate != null)
                    {
                        handleTemplate.Set(((SecurityBufferStruct*)outputBuffer.UnmanagedPointer)->token); // ATTN: on 64 BIT that is still +8 cause of 2* c++ unsigned long == 8 bytes
                        if (handleTemplate.IsInvalid)
                        {
                            handleTemplate.SetHandleAsInvalid();
                        }
                    }
                }

                if (inContextPtr == null && (errorCode & 0x80000000) != 0)
                {
                    // an error on the first call, need to set the out handle to invalid value
                    outContext._handle.SetToInvalid();
                }
            }

            return errorCode;
        }

        internal static unsafe int CompleteAuthToken(ref SafeDeleteContext refContext, SecurityBuffer[] inSecBuffers)
        {
            GlobalLog.Enter("SafeDeleteContext::CompleteAuthToken");
            GlobalLog.Print("    refContext       = " + ValidationHelper.ToString(refContext));
            GlobalLog.Assert(inSecBuffers != null, "SafeDeleteContext::CompleteAuthToken()|inSecBuffers == null");
            SecurityBufferDescriptor inSecurityBufferDescriptor = new SecurityBufferDescriptor(inSecBuffers.Length);

            int errorCode = (int)SecurityStatus.InvalidHandle;

            // these are pinned user byte arrays passed along with SecurityBuffers
            GCHandle[] pinnedInBytes = null;

            SecurityBufferStruct[] inUnmanagedBuffer = new SecurityBufferStruct[inSecurityBufferDescriptor.Count];
            fixed (void* inUnmanagedBufferPtr = inUnmanagedBuffer)
            {
                // Fix Descriptor pointer that points to unmanaged SecurityBuffers
                inSecurityBufferDescriptor.UnmanagedPointer = inUnmanagedBufferPtr;
                pinnedInBytes = new GCHandle[inSecurityBufferDescriptor.Count];
                SecurityBuffer securityBuffer;
                for (int index = 0; index < inSecurityBufferDescriptor.Count; ++index)
                {
                    securityBuffer = inSecBuffers[index];
                    if (securityBuffer != null)
                    {
                        inUnmanagedBuffer[index].count = securityBuffer.size;
                        inUnmanagedBuffer[index].type = securityBuffer.type;

                        // use the unmanaged token if it's not null; otherwise use the managed buffer
                        if (securityBuffer.unmanagedToken != null)
                        {
                            inUnmanagedBuffer[index].token = securityBuffer.unmanagedToken.DangerousGetHandle();
                        }
                        else if (securityBuffer.token == null || securityBuffer.token.Length == 0)
                        {
                            inUnmanagedBuffer[index].token = IntPtr.Zero;
                        }
                        else
                        {
                            pinnedInBytes[index] = GCHandle.Alloc(securityBuffer.token, GCHandleType.Pinned);
                            inUnmanagedBuffer[index].token = Marshal.UnsafeAddrOfPinnedArrayElement(securityBuffer.token, securityBuffer.offset);
                        }
                    }
                }

                SSPIHandle contextHandle = new SSPIHandle();
                if (refContext != null)
                {
                    contextHandle = refContext._handle;
                }
                try
                {
                    if (refContext == null || refContext.IsInvalid)
                    {
                        refContext = new SafeDeleteContext();
                    }

                    bool b = false;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        refContext.DangerousAddRef(ref b);
                    }
                    catch (Exception e)
                    {
                        if (b)
                        {
                            refContext.DangerousRelease();
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
                            errorCode = UnsafeNclNativeMethods.SafeNetHandles.CompleteAuthToken(contextHandle.IsZero ? null : &contextHandle, inSecurityBufferDescriptor);
                            refContext.DangerousRelease();
                        }
                    }
                }
                finally
                {
                    if (pinnedInBytes != null)
                    {
                        for (int index = 0; index < pinnedInBytes.Length; index++)
                        {
                            if (pinnedInBytes[index].IsAllocated)
                            {
                                pinnedInBytes[index].Free();
                            }
                        }
                    }
                }
            }

            GlobalLog.Leave("SafeDeleteContext::CompleteAuthToken() unmanaged CompleteAuthToken()", "errorCode:0x" + errorCode.ToString("x8") + " refContext:" + ValidationHelper.ToString(refContext));

            return errorCode;
        }
        
        protected override bool ReleaseHandle()
        {
            if (this._effectiveCredential != null)
            {
                this._effectiveCredential.DangerousRelease();
            }

            return UnsafeNclNativeMethods.SafeNetHandles.DeleteSecurityContext(ref _handle) == 0;
        }
    }
}
