// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic
{
    internal class QuicException : Exception
    {
        public QuicException(string message)
            : base (message)
        {
        }
    }
}
