// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal static class MsQuicConstants
    {
        private const uint Success = 0;

        internal static Func<uint, string> ErrorTypeFromErrorCode = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Windows.GetError : (Func<uint, string>)Linux.GetError;
        
        internal static class Windows
        {
            private const uint Pending = 0x703E5;
            private const uint Continue = 0x704DE;
            private const uint OutOfMemory = 0x8007000E;
            private const uint InvalidParameter = 0x80070057;
            private const uint InvalidState = 0x8007139F;
            private const uint NotSupported = 0x80004002;
            private const uint NotFound = 0x80070490;
            private const uint BufferTooSmall = 0x8007007A;
            private const uint HandshakeFailure = 0x80410000;
            private const uint Aborted = 0x80004004;
            private const uint AddressInUse = 0x80072740;
            private const uint ConnectionTimeout = 0x800704CF;
            private const uint ConnectionIdle = 0x800704D4;
            private const uint InternalError = 0x80004005;
            private const uint ServerBusy = 0x800704C9;
            private const uint ProtocolError = 0x800704CD;
            private const uint VerNegError = 0x80410001;

            public static string GetError(uint status)
            {
                switch (status)
                {
                    case Success:
                        return "SUCCESS";
                    case Pending:
                        return "PENDING";
                    case Continue:
                        return "CONTINUE";
                    case OutOfMemory:
                        return "OUT_OF_MEMORY";
                    case InvalidParameter:
                        return "INVALID_PARAMETER";
                    case InvalidState:
                        return "INVALID_STATE";
                    case NotSupported:
                        return "NOT_SUPPORTED";
                    case NotFound:
                        return "NOT_FOUND";
                    case BufferTooSmall:
                        return "BUFFER_TOO_SMALL";
                    case HandshakeFailure:
                        return "HANDSHAKE_FAILURE";
                    case Aborted:
                        return "ABORTED";
                    case AddressInUse:
                        return "ADDRESS_IN_USE";
                    case ConnectionTimeout:
                        return "CONNECTION_TIMEOUT";
                    case ConnectionIdle:
                        return "CONNECTION_IDLE";
                    case InternalError:
                        return "INTERNAL_ERROR";
                    case ServerBusy:
                        return "SERVER_BUSY";
                    case ProtocolError:
                        return "PROTOCOL_ERROR";
                    case VerNegError:
                        return "VER_NEG_ERROR";
                }
                return status.ToString();
            }
        }

        internal static class Linux
        {
            private const uint Pending = unchecked((uint)-2);
            private const uint Continue = unchecked((uint)-1);
            private const uint OutOfMemory = 12;
            private const uint InvalidParameter = 22;
            private const uint InvalidState = 200000002;
            private const uint NotSupported = 95;
            private const uint NotFound = 2;
            private const uint BufferTooSmall = 75;
            private const uint HandshakeFailure = 200000009;
            private const uint Aborted = 200000008;
            private const uint AddressInUse = 98;
            private const uint ConnectionTimeout = 110;
            private const uint ConnectionIdle = 200000011;
            private const uint InternalError = 200000012;
            private const uint ServerBusy = 200000007;
            private const uint ProtocolError = 200000013;
            private const uint VerNegError = 200000014;


            public static string GetError(uint status)
            {
                switch (status)
                {
                    case Success:
                        return "SUCCESS";
                    case Pending:
                        return "PENDING";
                    case Continue:
                        return "CONTINUE";
                    case OutOfMemory:
                        return "OUT_OF_MEMORY";
                    case InvalidParameter:
                        return "INVALID_PARAMETER";
                    case InvalidState:
                        return "INVALID_STATE";
                    case NotSupported:
                        return "NOT_SUPPORTED";
                    case NotFound:
                        return "NOT_FOUND";
                    case BufferTooSmall:
                        return "BUFFER_TOO_SMALL";
                    case HandshakeFailure:
                        return "HANDSHAKE_FAILURE";
                    case Aborted:
                        return "ABORTED";
                    case AddressInUse:
                        return "ADDRESS_IN_USE";
                    case ConnectionTimeout:
                        return "CONNECTION_TIMEOUT";
                    case ConnectionIdle:
                        return "CONNECTION_IDLE";
                    case InternalError:
                        return "INTERNAL_ERROR";
                    case ServerBusy:
                        return "SERVER_BUSY";
                    case ProtocolError:
                        return "PROTOCOL_ERROR";
                    case VerNegError:
                        return "VER_NEG_ERROR";
                }

                return status.ToString();       
            }
        }
    }
}
