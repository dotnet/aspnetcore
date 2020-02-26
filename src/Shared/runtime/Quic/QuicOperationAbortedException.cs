// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Net.Quic
{
    internal class QuicOperationAbortedException : QuicException
    {
        internal QuicOperationAbortedException()
            : base(SR.net_quic_operationaborted)
        {
        }

        public QuicOperationAbortedException(string message) : base(message)
        {
        }
    }
}
