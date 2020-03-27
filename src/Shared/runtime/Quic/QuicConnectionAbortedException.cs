// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Quic
{
    internal class QuicConnectionAbortedException : QuicException
    {
        internal QuicConnectionAbortedException(long errorCode)
            : this(SR.Format(SR.net_quic_connectionaborted, errorCode), errorCode)
        {
        }

        public QuicConnectionAbortedException(string message, long errorCode)
            : base (message)
        {
            ErrorCode = errorCode;
        }

        public long ErrorCode { get; }
    }
}
