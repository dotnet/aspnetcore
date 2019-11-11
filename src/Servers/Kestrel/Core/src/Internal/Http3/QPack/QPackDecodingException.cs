// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
    [Serializable]
    internal class QPackDecodingException : Exception
    {
        public QPackDecodingException()
        {
        }

        public QPackDecodingException(string message) : base(message)
        {
        }

        public QPackDecodingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected QPackDecodingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
