// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Mcp.Hosting;

internal sealed class McpNavigationManager : NavigationManager
{
    private readonly McpOperationContext _context;

    public McpNavigationManager(McpOperationContext context)
    {
        _context = context;
        Initialize(context.BaseUri.AbsoluteUri, context.Request.Location.AbsoluteUri);
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        var target = ToAbsoluteUri(uri);
        _context.Redirect = target;
        _context.ReplaceHistory = options.ReplaceHistoryEntry;
        if (!AppContext.TryGetSwitch("Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException", out var disableThrow)
            || !disableThrow)
        {
            throw new NavigationException(target.AbsoluteUri);
        }
    }
}
