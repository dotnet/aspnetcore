// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal class Http3ConnectionErrorException : Exception
    {
        public Http3ConnectionErrorException(string message, Http3ErrorCode errorCode)
            : base($"HTTP/3 connection error ({errorCode}): {message}")
        {
            ErrorCode = errorCode;
        }

        public Http3ErrorCode ErrorCode { get; }
    }
}
