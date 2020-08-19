// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    [Serializable]
    internal class Http3ConnectionException : Exception
    {
        public Http3ConnectionException()
        {
        }

        public Http3ConnectionException(string message) : base(message)
        {
        }

        public Http3ConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected Http3ConnectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}