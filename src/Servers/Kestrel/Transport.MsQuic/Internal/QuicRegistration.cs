// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class QuicRegistration : IDisposable
    {
        private bool _disposed = false;

        public IntPtr RegistrationContext { get; set; }

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllName);

        [DllImport("kernel32.dll")]
        public static extern IntPtr FreeLibrary(string dllName);

        public unsafe QuicRegistration()
        {
            var status = (QUIC_STATUS)NativeMethods.MsQuicOpen(version: 1, out var registration);
            QuicStatusException.ThrowIfFailed(status);

            NativeRegistration = *registration;

            RegistrationOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.RegistrationOpenDelegate>(
                    NativeRegistration.RegistrationOpen);
            RegistrationCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.RegistrationCloseDelegate>(
                    NativeRegistration.RegistrationClose);

            SecConfigCreateDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.SecConfigCreateDelegate>(
                    NativeRegistration.SecConfigCreate);
            SecConfigDeleteDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.SecConfigDeleteDelegate>(
                    NativeRegistration.SecConfigDelete);

            SessionOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.SessionOpenDelegate>(
                    NativeRegistration.SessionOpen);
            SessionCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.SessionCloseDelegate>(
                    NativeRegistration.SessionClose);
            SessionShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.SessionShutdownDelegate>(
                    NativeRegistration.SessionShutdown);

            ListenerOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.ListenerOpenDelegate>(
                    NativeRegistration.ListenerOpen);
            ListenerCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.ListenerCloseDelegate>(
                    NativeRegistration.ListenerClose);
            ListenerStartDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.ListenerStartDelegate>(
                    NativeRegistration.ListenerStart);
            ListenerStopDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.ListenerStopDelegate>(
                    NativeRegistration.ListenerStop);

            ConnectionOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.ConnectionOpenDelegate>(
                    NativeRegistration.ConnectionOpen);
            ConnectionCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.ConnectionCloseDelegate>(
                    NativeRegistration.ConnectionClose);
            ConnectionShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.ConnectionShutdownDelegate>(
                    NativeRegistration.ConnectionShutdown);
            ConnectionStartDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.ConnectionStartDelegate>(
                    NativeRegistration.ConnectionStart);

            StreamOpenDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.StreamOpenDelegate>(
                    NativeRegistration.StreamOpen);
            StreamCloseDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.StreamCloseDelegate>(
                    NativeRegistration.StreamClose);
            StreamStartDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.StreamStartDelegate>(
                   NativeRegistration.StreamStart);
            StreamShutdownDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.StreamShutdownDelegate>(
                    NativeRegistration.StreamShutdown);
            StreamSendDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.StreamSendDelegate>(
                    NativeRegistration.StreamSend);

            SetContextDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.SetContextDelegate>(
                    NativeRegistration.SetContext);
            GetContextDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.GetContextDelegate>(
                    NativeRegistration.GetContext);
            SetCallbackHandlerDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.SetCallbackHandlerDelegate>(
                    NativeRegistration.SetCallbackHandler);

            SetParamDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.SetParamDelegate>(
                    NativeRegistration.SetParam);
            GetParamDelegate =
                Marshal.GetDelegateForFunctionPointer<NativeMethods.GetParamDelegate>(
                    NativeRegistration.GetParam);
        }

        internal NativeMethods.NativeRegistration NativeRegistration { get; private set; }

        internal NativeMethods.RegistrationOpenDelegate RegistrationOpenDelegate { get; private set; }
        internal NativeMethods.RegistrationCloseDelegate RegistrationCloseDelegate { get; private set; }

        internal NativeMethods.SecConfigCreateDelegate SecConfigCreateDelegate { get; private set; }
        internal NativeMethods.SecConfigCreateCompleteDelegate SecConfigCreateCompleteDelegate { get; private set; }
        internal NativeMethods.SecConfigDeleteDelegate SecConfigDeleteDelegate { get; private set; }

        internal NativeMethods.SessionOpenDelegate SessionOpenDelegate { get; private set; }
        internal NativeMethods.SessionCloseDelegate SessionCloseDelegate { get; private set; }
        internal NativeMethods.SessionShutdownDelegate SessionShutdownDelegate { get; private set; }

        internal NativeMethods.ListenerOpenDelegate ListenerOpenDelegate { get; private set; }
        internal NativeMethods.ListenerCloseDelegate ListenerCloseDelegate { get; private set; }
        internal NativeMethods.ListenerStartDelegate ListenerStartDelegate { get; private set; }
        internal NativeMethods.ListenerStopDelegate ListenerStopDelegate { get; private set; }

        internal NativeMethods.ConnectionOpenDelegate ConnectionOpenDelegate { get; private set; }
        internal NativeMethods.ConnectionCloseDelegate ConnectionCloseDelegate { get; private set; }
        internal NativeMethods.ConnectionShutdownDelegate ConnectionShutdownDelegate { get; private set; }
        internal NativeMethods.ConnectionStartDelegate ConnectionStartDelegate { get; private set; }

        internal NativeMethods.StreamOpenDelegate StreamOpenDelegate { get; private set; }
        internal NativeMethods.StreamCloseDelegate StreamCloseDelegate { get; private set; }
        internal NativeMethods.StreamStartDelegate StreamStartDelegate { get; private set; }
        internal NativeMethods.StreamShutdownDelegate StreamShutdownDelegate { get; private set; }
        internal NativeMethods.StreamSendDelegate StreamSendDelegate { get; private set; }
        internal NativeMethods.StreamReceiveCompleteDelegate StreamReceiveComplete { get; private set; }

        internal NativeMethods.SetContextDelegate SetContextDelegate { get; private set; }
        internal NativeMethods.GetContextDelegate GetContextDelegate { get; private set; }
        internal NativeMethods.SetCallbackHandlerDelegate SetCallbackHandlerDelegate { get; private set; }

        internal NativeMethods.SetParamDelegate SetParamDelegate { get; private set; }
        internal NativeMethods.GetParamDelegate GetParamDelegate { get; private set; }

        public void RegistrationOpen(byte[] name)
        {
            RegistrationOpenDelegate(name, out var ctx);
            RegistrationContext = ctx;
        }

        public async ValueTask<QuicSecConfig> CreateSecurityConfig(X509Certificate2 certificate)
        {
            QuicSecConfig secConfig = null;
            var tcs = new TaskCompletionSource<object>();
            var secConfigCreateStatus = QUIC_STATUS.INTERNAL_ERROR;


            var status = SecConfigCreateDelegate(
                RegistrationContext,
                (uint)QUIC_SEC_CONFIG_FLAG.CERT_CONTEXT,
                certificate.Handle,
                null,
                IntPtr.Zero,
                SecCfgCreateCallbackHandler);

            QuicStatusException.ThrowIfFailed(status);

            void SecCfgCreateCallbackHandler(
                IntPtr context,
                QUIC_STATUS status,
                IntPtr securityConfig)
            {
                if (status.HasSucceeded())
                {
                    // TODO should we check if outer is disposed here?
                    secConfig = new QuicSecConfig(this, securityConfig);
                }

                secConfigCreateStatus = status;
                tcs.SetResult(null);
            }

            await tcs.Task;

            QuicStatusException.ThrowIfFailed(secConfigCreateStatus);

            return secConfig;
        }

        public QuicSession SessionOpen(
           string alpn)
        {
            var buffer = Encoding.UTF8.GetBytes(alpn);
            var sessionPtr = IntPtr.Zero;
            try
            {
                var status = (QUIC_STATUS)SessionOpenDelegate(
                    RegistrationContext,
                    buffer,
                    IntPtr.Zero,
                    ref sessionPtr);
                QuicStatusException.ThrowIfFailed(status);
            }
            catch
            {
                throw;
            }
            var session = new QuicSession(this, sessionPtr, buffer);
            return session;
        }

        public long Handle { get => (long)RegistrationContext; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal unsafe QUIC_STATUS UnsafeSetParam(
            IntPtr Handle,
            uint Level,
            uint Param,
            NativeMethods.QuicBuffer Buffer)
        {
            return SetParamDelegate(
                Handle,
                Level,
                Param,
                Buffer.Length,
                Buffer.Buffer);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(QuicRegistration));
            }
        }

        ~QuicRegistration()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            RegistrationCloseDelegate?.Invoke(RegistrationContext);

            _disposed = true;
        }
    }
}
