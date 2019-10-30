// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class QuicStatusException : Exception
    {
        internal static void ThrowIfFailed(QUIC_STATUS status, string message = null, Exception innerException = null)
        {
            if (!status.Succeeded())
            {
                throw new QuicStatusException(status, message, innerException);
            }
        }

        internal QuicStatusException(QUIC_STATUS status)
            : base()
        {
            Status = status;
        }

        internal QuicStatusException(QUIC_STATUS status, string message)
            : base(message)
        {
            Status = status;
        }

        internal QuicStatusException(QUIC_STATUS status, string message, Exception innerException)
            : base(message, innerException)
        {
            Status = status;
        }

        internal QUIC_STATUS Status { get; }

        public override string Message => string.Format(CultureInfo.InvariantCulture,
            "Status=[{0}].", Status);
    }
}
