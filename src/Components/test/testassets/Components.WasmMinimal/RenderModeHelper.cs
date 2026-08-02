
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

public static class RenderModeHelper
{
    public static IComponentRenderMode GetRenderMode(RenderModeId renderMode)
    {
        return renderMode switch
        {
            RenderModeId.ServerPrerendered => RenderMode.InteractiveServer,
            RenderModeId.ServerNonPrerendered => new InteractiveServerRenderMode(false),
            RenderModeId.WebAssemblyPrerendered => RenderMode.InteractiveWebAssembly,
            RenderModeId.WebAssemblyNonPrerendered => new InteractiveWebAssemblyRenderMode(false),
            RenderModeId.AutoPrerendered => RenderMode.InteractiveAuto,
            RenderModeId.AutoNonPrerendered => new InteractiveAutoRenderMode(false),
            _ => throw new InvalidOperationException($"Unknown render mode: {renderMode}"),
        };
    }

    public static RenderModeId ParseRenderMode(string? renderModeStr)
    {
        if (!string.IsNullOrEmpty(renderModeStr) &&
            Enum.TryParse<RenderModeId>(renderModeStr, ignoreCase: true, out var result))
        {
            return result;
        }
        return RenderModeId.AutoNonPrerendered;
    }

}

public enum RenderModeId
{
    ServerPrerendered = 0,
    ServerNonPrerendered = 1,
    WebAssemblyPrerendered = 2,
    WebAssemblyNonPrerendered = 3,
    AutoPrerendered = 4,
    AutoNonPrerendered = 5,
}
