// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class QuicExceptionHelpers
    {
        internal static void ThrowIfFailed(uint status, string message = null, Exception innerException = null)
        {
            if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
            {
                throw new QuicException($"{message} Error Code: {MsQuicStatusCodes.GetError(status)}");
            }
        }
    }
}
