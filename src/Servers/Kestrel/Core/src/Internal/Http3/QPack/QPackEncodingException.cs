// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
    internal sealed class QPackEncodingException : Exception
    {
        public QPackEncodingException(string message)
            : base(message)
        {
        }
        public QPackEncodingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
