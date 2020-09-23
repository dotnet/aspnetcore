// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class QuicExceptionHelpers
    {
        internal static void ThrowIfFailed(uint status, string? message = null, Exception? innerException = null)
        {
            if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
            {
                throw CreateExceptionForHResult(status, message, innerException);
            }
        }

        internal static Exception CreateExceptionForHResult(uint status, string? message = null, Exception? innerException = null)
        {
            return new QuicException($"{message} Error Code: {MsQuicStatusCodes.GetError(status)}", innerException);
        }
    }
}
