// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class MsQuicApi : IDisposable
    {
        private bool _disposed = false;

        private IntPtr _registrationContext;

        internal unsafe MsQuicApi()
        {
            var status = (uint)NativeMethods.MsQuicOpen(version: 1, out var registration);
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

        internal NativeMethods.NativeApi NativeRegistration { get; private set; }

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

        internal void RegistrationOpen(byte[] name)
        {
            QuicStatusException.ThrowIfFailed(RegistrationOpenDelegate(name, out var ctx));
            _registrationContext = ctx;
        }

        internal unsafe uint UnsafeSetParam(
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

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~MsQuicApi()
        {
            Dispose(disposing: false);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            RegistrationCloseDelegate?.Invoke(_registrationContext);

            _disposed = true;
        }
    }
}
