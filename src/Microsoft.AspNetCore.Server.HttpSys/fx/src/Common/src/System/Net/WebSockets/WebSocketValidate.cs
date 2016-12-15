// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text;

namespace System.Net.WebSockets
{
    internal static class WebSocketValidate
    {
        internal const int MaxControlFramePayloadLength = 123;
        private const int CloseStatusCodeAbort = 1006;
        private const int CloseStatusCodeFailedTLSHandshake = 1015;
        private const int InvalidCloseStatusCodesFrom = 0;
        private const int InvalidCloseStatusCodesTo = 999;
        private const string Separators = "()<>@,;:\\\"/[]?={} ";

        internal static void ValidateSubprotocol(string subProtocol)
        {
            if (string.IsNullOrWhiteSpace(subProtocol))
            {
                throw new ArgumentException(SR.net_WebSockets_InvalidEmptySubProtocol, nameof(subProtocol));
            }

            string invalidChar = null;
            int i = 0;
            while (i < subProtocol.Length)
            {
                char ch = subProtocol[i];
                if (ch < 0x21 || ch > 0x7e)
                {
                    invalidChar = string.Format(CultureInfo.InvariantCulture, "[{0}]", (int)ch);
                    break;
                }

                if (!char.IsLetterOrDigit(ch) &&
                    Separators.IndexOf(ch) >= 0)
                {
                    invalidChar = ch.ToString();
                    break;
                }

                i++;
            }

            if (invalidChar != null)
            {
                throw new ArgumentException(SR.Format(SR.net_WebSockets_InvalidCharInProtocolString, subProtocol, invalidChar), nameof(subProtocol));
            }
        }

        internal static void ValidateCloseStatus(WebSocketCloseStatus closeStatus, string statusDescription)
        {
            if (closeStatus == WebSocketCloseStatus.Empty && !string.IsNullOrEmpty(statusDescription))
            {
                throw new ArgumentException(SR.Format(SR.net_WebSockets_ReasonNotNull,
                    statusDescription,
                    WebSocketCloseStatus.Empty),
                    nameof(statusDescription));
            }

            int closeStatusCode = (int)closeStatus;

            if ((closeStatusCode >= InvalidCloseStatusCodesFrom &&
                closeStatusCode <= InvalidCloseStatusCodesTo) ||
                closeStatusCode == CloseStatusCodeAbort ||
                closeStatusCode == CloseStatusCodeFailedTLSHandshake)
            {
                // CloseStatus 1006 means Aborted - this will never appear on the wire and is reflected by calling WebSocket.Abort
                throw new ArgumentException(SR.Format(SR.net_WebSockets_InvalidCloseStatusCode,
                    closeStatusCode),
                    nameof(closeStatus));
            }

            int length = 0;
            if (!string.IsNullOrEmpty(statusDescription))
            {
                length = Encoding.UTF8.GetByteCount(statusDescription);
            }

            if (length > MaxControlFramePayloadLength)
            {
                throw new ArgumentException(SR.Format(SR.net_WebSockets_InvalidCloseStatusDescription,
                    statusDescription,
                    MaxControlFramePayloadLength),
                    nameof(statusDescription));
            }
        }

        internal static void ThrowPlatformNotSupportedException()
        {
            throw new PlatformNotSupportedException(SR.net_WebSockets_UnsupportedPlatform);
        }

        internal static void ValidateArraySegment(ArraySegment<byte> arraySegment, string parameterName)
        {
            if (arraySegment.Array == null)
            {
                throw new ArgumentNullException(parameterName + ".Array");
            }
        }

        internal static void ThrowIfInvalidState(WebSocketState currentState, bool isDisposed, WebSocketState[] validStates)
        {
            string validStatesText = string.Empty;

            if (validStates != null && validStates.Length > 0)
            {
                foreach (WebSocketState validState in validStates)
                {
                    if (currentState == validState)
                    {
                        // Ordering is important to maintain .NET 4.5 WebSocket implementation exception behavior.
                        if (isDisposed)
                        {
                            throw new ObjectDisposedException("ClientWebSocket");
                        }

                        return;
                    }
                }

                validStatesText = string.Join(", ", validStates);
            }

            throw new WebSocketException(
                WebSocketError.InvalidState,
                SR.Format(SR.net_WebSockets_InvalidState, currentState, validStatesText));
        }
    }
}
