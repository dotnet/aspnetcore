// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Connections;

namespace TestServer;

public class WebSocketCompressionConfiguration
{
    public bool IsCompressionDisabled { get; set; }

    public string CspPolicy { get; set; } = "'self'";

    public Func<HttpContext, WebSocketAcceptContext, Task> ConfigureWebSocketAcceptContext { get; set; }
}
