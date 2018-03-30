// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubOptions
    {
        // HandshakeTimeout and KeepAliveInterval are set to null here to help identify when
        // local hub options have been set. Global default values are set in HubOptionsSetup.
        // SupportedProtocols being null is the true default value, and it represents support
        // for all available protocols.
        public TimeSpan? HandshakeTimeout { get; set; } = null;

        public TimeSpan? KeepAliveInterval { get; set; } = null;

        public IList<string> SupportedProtocols { get; set; } = null;

        public bool? EnableDetailedErrors { get; set; } = null;
    }
}
