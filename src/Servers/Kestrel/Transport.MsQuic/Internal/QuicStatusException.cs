// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.MsQuic.Internal
{
    internal class QuicStatusException : Exception
    {
        internal QuicStatusException(uint status)
            : this(status, null)
        {
        }

        internal QuicStatusException(uint status, string message)
            : this(status, message, null)
        {
        }

        internal QuicStatusException(uint status, string message, Exception innerException)
            : base(GetMessage(status, message), innerException)
        {
            Status = status;
        }

        internal uint Status { get; }

        private static string GetMessage(uint status, string message)
        {
            var errorCode = MsQuicConstants.ErrorTypeFromErrorCode(status);
            return $"Quic Error: {errorCode}. " + message;
        }

        internal static void ThrowIfFailed(uint status, string message = null, Exception innerException = null)
        {
            if (!status.Succeeded())
            {
                throw new QuicStatusException(status, message, innerException);
            }
        }
    }
}
