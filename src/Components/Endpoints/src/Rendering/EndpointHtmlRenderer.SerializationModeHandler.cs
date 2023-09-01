// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class EndpointHtmlRenderer : ISerializationModeHandler
{
    private PersistedStateSerializationMode _serializationMode = PersistedStateSerializationMode.Infer;

    PersistedStateSerializationMode ISerializationModeHandler.GlobalSerializationMode
    {
        get => _serializationMode;
        set
        {
            if (value != PersistedStateSerializationMode.Server)
            {
                throw new InvalidOperationException("Can only change global serialization mode to Server.");
            }
            _serializationMode = value;
        }
    }

    public PersistedStateSerializationMode GetCallbackTargetSerializationMode(object? callbackTarget)
    {
        // This happens on the circuit after changing the GlobalSerializationMode
        if (_serializationMode == PersistedStateSerializationMode.Server)
        {
            return _serializationMode;
        }

        // Otherwise infer the component's render mode
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
                _ => throw new InvalidOperationException("Invalid render mode.")
            };
    }
}
