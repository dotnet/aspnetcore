// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Http.Connections
{
    public class WebSocketOptions
    {
        public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);

        public string SubProtocol { get; set; }
    }
}
