// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Authorization;

namespace Microsoft.AspNetCore.Components.Mcp.Hosting;

internal sealed class McpAuthenticationStateProvider(McpOperationContext context) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(context.Request.User));
}
