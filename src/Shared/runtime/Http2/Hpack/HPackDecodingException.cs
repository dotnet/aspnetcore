// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Net.Http.HPack
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

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HPackDecodingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
