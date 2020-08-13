// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class WebSocketsSupportedConditionAttribute : Attribute, ITestCondition
    {
        public bool IsMet => TestHelpers.IsWebSocketsSupported();

        public string SkipReason => "No WebSockets Client for this platform";
    }
}
