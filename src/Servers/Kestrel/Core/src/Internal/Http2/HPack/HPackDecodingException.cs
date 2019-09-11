// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
   // TODO: Should this be public?
    [Serializable]
    internal sealed class HPackDecodingException : Exception
    {
        public HPackDecodingException()
        {
        }

        public HPackDecodingException(string message) : base(message)
        {
        }

        public HPackDecodingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public HPackDecodingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
