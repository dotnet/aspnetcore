// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class EndpointHtmlRenderer : ISerializationModeHandler
{
    public PersistedStateSerializationMode GetCallbackTargetSerializationMode(object? callbackTarget)
    {
        if (callbackTarget is not IComponent)
        {
            throw new InvalidOperationException("Cannot infer serialization mode for non component. Provide a serialization mode.");
        }

        var component = (IComponent)callbackTarget;
        var ssrRenderBoundary = GetClosestRenderModeBoundary(component);

        return ssrRenderBoundary is null
            ? throw new InvalidCastException("Cannot infer serialization mode.")
            : ssrRenderBoundary.RenderMode switch
            {
                ServerRenderMode => PersistedStateSerializationMode.Server,
                WebAssemblyRenderMode => PersistedStateSerializationMode.WebAssembly,
                AutoRenderMode => PersistedStateSerializationMode.Infer,
                _ => throw new InvalidOperationException("Invalid render mode.")
            };
    }
}
