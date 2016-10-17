// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.WebSockets.Internal.Tests
{
    public class WebSocketConnectionSummary
    {
        public IList<WebSocketFrame> Received { get; }
        public WebSocketCloseResult CloseResult { get; }

        public WebSocketConnectionSummary(IList<WebSocketFrame> received, WebSocketCloseResult closeResult)
        {
            Received = received;
            CloseResult = closeResult;
        }
    }
}