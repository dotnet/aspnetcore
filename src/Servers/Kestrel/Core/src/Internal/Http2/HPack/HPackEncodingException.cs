// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    internal sealed class HPackEncodingException : Exception
    {
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
