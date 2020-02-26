// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
