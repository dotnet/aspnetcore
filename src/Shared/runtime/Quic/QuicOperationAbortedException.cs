// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
