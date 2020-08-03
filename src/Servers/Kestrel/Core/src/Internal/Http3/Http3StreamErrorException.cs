// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    class Http3StreamErrorException : Exception
    {
        public Http3StreamErrorException(string message, Http3ErrorCode errorCode)
            : base($"HTTP/3 stream error ({errorCode}): {message}")
        {
            ErrorCode = errorCode;
        }

        public Http3ErrorCode ErrorCode { get; }
    }
}
