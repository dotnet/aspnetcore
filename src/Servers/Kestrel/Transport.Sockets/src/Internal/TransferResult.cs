// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal readonly struct TransferResult
    {
        public readonly SocketException? SocketError;

        public readonly int BytesTransferred;

        public readonly bool HasError;

        public TransferResult(int bytesTransferred)
        {
            SocketError = null;
            BytesTransferred = bytesTransferred;
            HasError = false;
        }

        public TransferResult(SocketException exception)
        {
            SocketError = exception;
            BytesTransferred = 0;
            HasError = true;
        }
    }
}