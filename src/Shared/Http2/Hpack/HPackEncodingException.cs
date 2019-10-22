// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0.
// See THIRD-PARTY-NOTICES.TXT in the project root for license information.

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
