// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Net.Http.HPack
{
    // TODO: Should this be public?
    [Serializable]
    internal class HPackDecodingException : Exception
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
