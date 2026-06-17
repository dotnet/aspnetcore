// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.SignalR.Tests;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class WebSocketsSupportedConditionAttribute : Attribute, ITestCondition
{
    public bool IsMet => TestHelpers.IsWebSocketsSupported();

    public string SkipReason => "No WebSockets Client for this platform";
}
