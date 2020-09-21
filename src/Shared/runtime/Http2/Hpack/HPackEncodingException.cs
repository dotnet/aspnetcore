// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http.HPack
{
    internal sealed class HPackEncodingException : Exception
    {
        public HPackEncodingException()
        {
        }

        public HPackEncodingException(string message)
            : base(message)
        {
        }

        public HPackEncodingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
