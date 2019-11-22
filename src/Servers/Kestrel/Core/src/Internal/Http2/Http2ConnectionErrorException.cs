// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal sealed class Http2ConnectionErrorException : Exception
    {
        public Http2ConnectionErrorException(string message, Http2ErrorCode errorCode)
            : base($"HTTP/2 connection error ({errorCode}): {message}")
        {
            ErrorCode = errorCode;
        }

        public Http2ErrorCode ErrorCode { get; }
    }
}
