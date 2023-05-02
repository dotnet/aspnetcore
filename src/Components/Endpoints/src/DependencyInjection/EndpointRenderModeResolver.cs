// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class EndpointRenderModeResolver : RenderModeResolver
{
    public override IComponent ResolveComponent(Type componentType, IComponentActivator componentActivator, IComponentRenderMode? componentTypeRenderMode, IComponentRenderMode? callSiteRenderMode)
        => (callSiteRenderMode, componentTypeRenderMode) switch
        {
            (SSRRenderModeBoundary.BypassRenderMode, _) => componentActivator.CreateInstance(componentType),
            ({ } mode, null) => new SSRRenderModeBoundary(componentType, mode),
            (null, { } mode) => new SSRRenderModeBoundary(componentType, mode),
            ({ }, { }) => throw new InvalidOperationException($"The component type '{componentType}' has a fixed rendermode of '{componentTypeRenderMode}', so it is not valid to specify a rendermode when using this component."),
            _ => throw new InvalidOperationException("No render mode was specified."), // This should never happen as the framework won't call here in this case
        };
}
