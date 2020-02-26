// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.Serialization;

namespace System.Net.Http.QPack
{
    [Serializable]
    internal sealed class QPackDecodingException : Exception
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

        private QPackDecodingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
