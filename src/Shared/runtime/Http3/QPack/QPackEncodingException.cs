// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Net.Http.QPack
{
    [Serializable]
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

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
#endif
        private QPackEncodingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
