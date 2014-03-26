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

//------------------------------------------------------------------------------
// <copyright file="UnsafeNativeMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.AspNet.Security.Windows
{
    [System.Security.SuppressUnmanagedCodeSecurityAttribute]
    internal static class UnsafeNclNativeMethods
    {
#if ASPNETCORE50
        private const string sspicli_LIB = "sspicli.dll";
        private const string api_ms_win_core_processthreads_LIB = "api-ms-win-core-processthreads-l1-1-1.dll";
        private const string api_ms_win_core_handle_LIB = "api-ms-win-core-handle-l1-1-0.dll";
        private const string api_ms_win_core_libraryloader_LIB = "api-ms-win-core-libraryloader-l1-1-1.dll";
        private const string api_ms_win_core_heap_obsolete_LIB = "api-ms-win-core-heap-obsolete-l1-1-0.dll";
#else
        private const string KERNEL32 = "kernel32.dll";
#endif
        private const string SECUR32 = "secur32.dll";
        private const string CRYPT32 = "crypt32.dll";

#if ASPNETCORE50
        [DllImport(api_ms_win_core_processthreads_LIB, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
#else
        [DllImport(KERNEL32, ExactSpelling = true, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
#endif
        internal static extern uint GetCurrentThreadId();


        [System.Security.SuppressUnmanagedCodeSecurityAttribute]
        internal static class SafeNetHandles
        {
            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static extern int FreeContextBuffer(
                [In] IntPtr contextBuffer);

            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static extern int FreeCredentialsHandle(
                  ref SSPIHandle handlePtr);

            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static extern int DeleteSecurityContext(
                  ref SSPIHandle handlePtr);

            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static unsafe extern int AcceptSecurityContext(
                      ref SSPIHandle credentialHandle,
                      [In] void* inContextPtr,
                      [In] SecurityBufferDescriptor inputBuffer,
                      [In] ContextFlags inFlags,
                      [In] Endianness endianness,
                      ref SSPIHandle outContextPtr,
                      [In, Out] SecurityBufferDescriptor outputBuffer,
                      [In, Out] ref ContextFlags attributes,
                      out long timeStamp);

            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static unsafe extern int QueryContextAttributesW(
                ref SSPIHandle contextHandle,
                [In] ContextAttribute attribute,
                [In] void* buffer);

            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static unsafe extern int SetContextAttributesW(
                ref SSPIHandle contextHandle,
                [In] ContextAttribute attribute,
                [In] byte[] buffer,
                [In] int bufferSize);

            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
            internal static extern int EnumerateSecurityPackagesW(
                [Out] out int pkgnum,
                [Out] out SafeFreeContextBuffer handle);

            [DllImport(SECUR32, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static unsafe extern int AcquireCredentialsHandleW(
                      [In] string principal,
                      [In] string moduleName,
                      [In] int usage,
                      [In] void* logonID,
                      [In] ref AuthIdentity authdata,
                      [In] void* keyCallback,
                      [In] void* keyArgument,
                      ref SSPIHandle handlePtr,
                      [Out] out long timeStamp);

            [DllImport(SECUR32, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static unsafe extern int AcquireCredentialsHandleW(
                      [In] string principal,
                      [In] string moduleName,
                      [In] int usage,
                      [In] void* logonID,
                      [In] IntPtr zero,
                      [In] void* keyCallback,
                      [In] void* keyArgument,
                      ref SSPIHandle handlePtr,
                      [Out] out long timeStamp);

            // Win7+
            [DllImport(SECUR32, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static unsafe extern int AcquireCredentialsHandleW(
                      [In] string principal,
                      [In] string moduleName,
                      [In] int usage,
                      [In] void* logonID,
                      [In] SafeSspiAuthDataHandle authdata,
                      [In] void* keyCallback,
                      [In] void* keyArgument,
                      ref SSPIHandle handlePtr,
                      [Out] out long timeStamp);

            [DllImport(SECUR32, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
            internal static unsafe extern int AcquireCredentialsHandleW(
                      [In] string principal,
                      [In] string moduleName,
                      [In] int usage,
                      [In] void* logonID,
                      [In] ref SecureCredential authData,
                      [In] void* keyCallback,
                      [In] void* keyArgument,
                      ref SSPIHandle handlePtr,
                      [Out] out long timeStamp);

            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static unsafe extern int InitializeSecurityContextW(
                      ref SSPIHandle credentialHandle,
                      [In] void* inContextPtr,
                      [In] byte* targetName,
                      [In] ContextFlags inFlags,
                      [In] int reservedI,
                      [In] Endianness endianness,
                      [In] SecurityBufferDescriptor inputBuffer,
                      [In] int reservedII,
                      ref SSPIHandle outContextPtr,
                      [In, Out] SecurityBufferDescriptor outputBuffer,
                      [In, Out] ref ContextFlags attributes,
                      out long timeStamp);

            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static unsafe extern int CompleteAuthToken(
                      [In] void* inContextPtr,
                      [In, Out] SecurityBufferDescriptor inputBuffers);

            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
            internal static extern int QuerySecurityContextToken(ref SSPIHandle phContext, [Out] out SafeCloseHandle handle);

#if ASPNETCORE50
            [DllImport(api_ms_win_core_handle_LIB, ExactSpelling = true, SetLastError = true)]
#else
            [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]
#endif
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static extern bool CloseHandle(IntPtr handle);

#if ASPNETCORE50
            [DllImport(api_ms_win_core_heap_obsolete_LIB, ExactSpelling = true, SetLastError = true)]
#else
            [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]
#endif
            internal static extern SafeLocalFree LocalAlloc(int uFlags, UIntPtr sizetdwBytes);

#if ASPNETCORE50
            [DllImport(api_ms_win_core_heap_obsolete_LIB, ExactSpelling = true, SetLastError = true)]
#else
            [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]
#endif
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static extern IntPtr LocalFree(IntPtr handle);

#if ASPNETCORE50
            [DllImport(api_ms_win_core_libraryloader_LIB, ExactSpelling = true, SetLastError = true)]
#else
            [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]
#endif
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static extern unsafe bool FreeLibrary([In] IntPtr hModule);

            [DllImport(CRYPT32, ExactSpelling = true, SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static extern void CertFreeCertificateChain(
                [In] IntPtr pChainContext);

            [DllImport(CRYPT32, ExactSpelling = true, SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static extern void CertFreeCertificateChainList(
                [In] IntPtr ppChainContext);

            [DllImport(CRYPT32, ExactSpelling = true, SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static extern bool CertFreeCertificateContext(      // Suppressing returned status check, it's always==TRUE,
                [In] IntPtr certContext);

#if ASPNETCORE50
            [DllImport(api_ms_win_core_heap_obsolete_LIB, ExactSpelling = true, SetLastError = true)]
#else
            [DllImport(KERNEL32, ExactSpelling = true, SetLastError = true)]
#endif
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            internal static extern IntPtr GlobalFree(IntPtr handle);
        }

        [System.Security.SuppressUnmanagedCodeSecurityAttribute]
        internal static class NativeNTSSPI
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
            internal static extern int EncryptMessage(
                  ref SSPIHandle contextHandle,
                  [In] uint qualityOfProtection,
                  [In, Out] SecurityBufferDescriptor inputOutput,
                  [In] uint sequenceNumber);

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
            internal static unsafe extern int DecryptMessage(
                  [In] ref SSPIHandle contextHandle,
                  [In, Out] SecurityBufferDescriptor inputOutput,
                  [In] uint sequenceNumber,
                       uint* qualityOfProtection);
        } // class UnsafeNclNativeMethods.NativeNTSSPI

        [SuppressUnmanagedCodeSecurityAttribute]
        internal static class SspiHelper
        {
            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            internal static unsafe extern SecurityStatus SspiFreeAuthIdentity(
                [In] IntPtr authData);

            [SuppressMessage("Microsoft.Security", "CA2118:ReviewSuppressUnmanagedCodeSecurityUsage", Justification = "Implementation requires unmanaged code usage")]
            [DllImport(SECUR32, ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
            internal static unsafe extern SecurityStatus SspiEncodeStringsAsAuthIdentity(
                [In] string userName,
                [In] string domainName,
                [In] string password,
                [Out] out SafeSspiAuthDataHandle authData);
        }
    }
}
