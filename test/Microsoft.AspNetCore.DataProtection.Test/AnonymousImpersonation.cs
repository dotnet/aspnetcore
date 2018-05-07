// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET461
using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Helpers for working with the anonymous Windows identity.
    /// </summary>
    internal static class AnonymousImpersonation
    {
        /// <summary>
        /// Performs an action while impersonated under the anonymous user (NT AUTHORITY\ANONYMOUS LOGIN).
        /// </summary>
        public static void Run(Action callback)
        {
            using (var threadHandle = ThreadHandle.OpenCurrentThreadHandle())
            {
                bool impersonated = false;
                try
                {
                    impersonated = ImpersonateAnonymousToken(threadHandle);
                    if (!impersonated)
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
                    callback();
                }
                finally
                {
                    if (impersonated && !RevertToSelf())
                    {
                        Environment.FailFast("RevertToSelf() returned false!");
                    }
                }
            }
        }

        [DllImport("advapi32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        private static extern bool ImpersonateAnonymousToken([In] ThreadHandle ThreadHandle);

        [DllImport("advapi32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        private static extern bool RevertToSelf();

        private sealed class ThreadHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private ThreadHandle()
                : base(ownsHandle: true)
            {
            }

            public static ThreadHandle OpenCurrentThreadHandle()
            {
                const int THREAD_ALL_ACCESS = 0x1FFFFF;
                var handle = OpenThread(
                    dwDesiredAccess: THREAD_ALL_ACCESS,
                    bInheritHandle: false,
#pragma warning disable CS0618 // Type or member is obsolete
                    dwThreadId: (uint)AppDomain.GetCurrentThreadId());
#pragma warning restore CS0618 // Type or member is obsolete
                CryptoUtil.AssertSafeHandleIsValid(handle);
                return handle;
            }

            protected override bool ReleaseHandle()
            {
                return CloseHandle(handle);
            }

            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            private static extern bool CloseHandle(
                [In] IntPtr hObject);

            [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
            private static extern ThreadHandle OpenThread(
                [In] uint dwDesiredAccess,
                [In] bool bInheritHandle,
                [In] uint dwThreadId);
        }
    }
}
#elif NETCOREAPP2_2
#else
#error Target framework needs to be updated
#endif
