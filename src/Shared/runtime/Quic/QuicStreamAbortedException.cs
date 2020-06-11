// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Quic
{
    internal class QuicStreamAbortedException : QuicException
    {
        internal QuicStreamAbortedException(long errorCode)
            : this(SR.Format(SR.net_quic_streamaborted, errorCode), errorCode)
        {
        }

        public QuicStreamAbortedException(string message, long errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public long ErrorCode { get; }
    }
}
