// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.WebSockets.Internal
{
    public class WebSocketException : Exception
    {
        public WebSocketException()
        {
        }

        public WebSocketException(string message) : base(message)
        {
        }

        public WebSocketException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}