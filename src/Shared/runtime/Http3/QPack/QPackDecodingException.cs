// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
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

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
        private QPackDecodingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
