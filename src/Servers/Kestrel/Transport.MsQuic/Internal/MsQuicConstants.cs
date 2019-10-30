// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal static class MsQuicConstants
    {
        private const uint Success = 0;

        private const uint PendingWindows = 0x703E5;
        private const uint ContinueWindows = 0x704DE;
        private const uint OutOfMemoryWindows = 0x8007000E;
        private const uint InvalidParameterWindows = 0x80070057;
        private const uint InvalidStateWindows = 0x8007139F;
        private const uint NotSupportedWindows = 0x80004002;
        private const uint NotFoundWindows = 0x80070490;
        private const uint BufferTooSmallWindows = 0x8007007A;
        private const uint HandshakeFailureWindows = 0x80410000;
        private const uint AbortedWindows = 0x80004004;
        private const uint AddressInUseWindows = 0x80072740;
        private const uint ConnectionTimeoutWindows = 0x800704CF;
        private const uint ConnectionIdleWindows = 0x800704D4;
        private const uint InternalErrorWindows = 0x80004005;
        private const uint ServerBusyWindows = 0x800704C9;
        private const uint ProtocolErrorWindows = 0x800704CD;
        private const uint VerNegErrorWindows = 0x80410001;

        private const uint PendingLinux = unchecked((uint)-2);
        private const uint ContinueLinux = unchecked((uint)-1);
        private const uint OutOfMemoryLinux = 12;
        private const uint InvalidParameterLinux = 22;
        private const uint InvalidStateLinux = 200000002;
        private const uint NotSupportedLinux = 95;
        private const uint NotFoundLinux = 2;
        private const uint BufferTooSmallLinux = 75;
        private const uint HandshakeFailureLinux = 200000009;
        private const uint AbortedLinux = 200000008;
        private const uint AddressInUseLinux = 98;
        private const uint ConnectionTimeoutLinux = 110;
        private const uint ConnectionIdleLinux = 200000011;
        private const uint InternalErrorLinux = 200000012;
        private const uint ServerBusyLinux = 200000007;
        private const uint ProtocolErrorLinux = 200000013;
        private const uint VerNegErrorLinux = 200000014;

        internal static Func<uint, string> ErrorTypeFromErrorCode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? GetErrorFromWindows : (Func<uint, string>)GetErrorFromLinux;

        public static string GetErrorFromWindows(uint status)
        {
            switch (status)
            {
                case Success:
                    return "SUCCESS";
                case PendingWindows:
                    return "PENDING";
                case ContinueWindows:
                    return "CONTINUE";
                case OutOfMemoryWindows:
                    return "OUT_OF_MEMORY";
                case InvalidParameterWindows:
                    return "INVALID_PARAMETER";
                case InvalidStateWindows:
                    return "INVALID_STATE";
                case NotSupportedWindows:
                    return "NOT_SUPPORTED";
                case NotFoundWindows:
                    return "NOT_FOUND";
                case BufferTooSmallWindows:
                    return "BUFFER_TOO_SMALL";
                case HandshakeFailureWindows:
                    return "HANDSHAKE_FAILURE";
                case AbortedWindows:
                    return "ABORTED";
                case AddressInUseWindows:
                    return "ADDRESS_IN_USE";
                case ConnectionTimeoutWindows:
                    return "CONNECTION_TIMEOUT";
                case ConnectionIdleWindows:
                    return "CONNECTION_IDLE";
                case InternalErrorWindows:
                    return "INTERNAL_ERROR";
                case ServerBusyWindows:
                    return "SERVER_BUSY";
                case ProtocolErrorWindows:
                    return "PROTOCOL_ERROR";
                case VerNegErrorWindows:
                    return "VER_NEG_ERROR";
            }

            return status.ToString();
        }

        public static string GetErrorFromLinux(uint status)
        {
            switch (status)
            {
                case Success:
                    return "SUCCESS";
                case PendingLinux:
                    return "PENDING";
                case ContinueLinux:
                    return "CONTINUE";
                case OutOfMemoryLinux:
                    return "OUT_OF_MEMORY";
                case InvalidParameterLinux:
                    return "INVALID_PARAMETER";
                case InvalidStateLinux:
                    return "INVALID_STATE";
                case NotSupportedLinux:
                    return "NOT_SUPPORTED";
                case NotFoundLinux:
                    return "NOT_FOUND";
                case BufferTooSmallLinux:
                    return "BUFFER_TOO_SMALL";
                case HandshakeFailureLinux:
                    return "HANDSHAKE_FAILURE";
                case AbortedLinux:
                    return "ABORTED";
                case AddressInUseLinux:
                    return "ADDRESS_IN_USE";
                case ConnectionTimeoutLinux:
                    return "CONNECTION_TIMEOUT";
                case ConnectionIdleLinux:
                    return "CONNECTION_IDLE";
                case InternalErrorLinux:
                    return "INTERNAL_ERROR";
                case ServerBusyLinux:
                    return "SERVER_BUSY";
                case ProtocolErrorLinux:
                    return "PROTOCOL_ERROR";
                case VerNegErrorLinux:
                    return "VER_NEG_ERROR";
            }

            return status.ToString();       
        }
    }
}
