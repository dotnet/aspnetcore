// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.HttpFeature;

namespace Microsoft.AspNet.Http.Core
{
    public class WebSocketAcceptContext : IWebSocketAcceptContext
    {
        public virtual string SubProtocol { get; set; }
    }
}