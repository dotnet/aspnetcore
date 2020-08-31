// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
namespace System.Net.Quic
{
    internal class QuicException : Exception
    {
        public QuicException(string? message)
            : base(message)
        {
        }
        public QuicException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
