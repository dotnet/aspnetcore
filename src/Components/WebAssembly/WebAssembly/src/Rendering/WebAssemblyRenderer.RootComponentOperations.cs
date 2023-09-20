// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering;

// Contains logic for root components that get dynamically updated in Blazor Web scenarios.
internal partial class WebAssemblyRenderer
{
    private Dictionary<int, ParameterView>? _latestRootComponentDirectParameters;

    internal Task HandleRootComponentAddOperationAsync([DynamicallyAccessedMembers(Component)] Type componentType, ParameterView parameters, string domElementSelector)
    {
        var componentId = AddRootComponent(componentType, domElementSelector);

        _latestRootComponentDirectParameters ??= new();
        _latestRootComponentDirectParameters[componentId] = parameters;

        return RenderRootComponentAsync(componentId, parameters);
    }

    private Task HandleRootComponentUpdateOperationAsync(int componentId, ParameterView newParameters, string domElementSelector)
    {
        if (_latestRootComponentDirectParameters is not null &&
            _latestRootComponentDirectParameters.TryGetValue(componentId, out var oldParameters) &&
            oldParameters.DefinitelyEquals(newParameters))
        {
            // The parameters haven't changed, so there's no work to do.
            return Task.CompletedTask;
        }
        else
        {
            // The component parameters have changed. Rather than update the existing instance, we'll dispose
            // it and replace it with a new one. This is to be consistent with Blazor Server which,
            // for security reasons, doesn't support updating root component parameters.
            var componentType = GetComponentState(componentId).Component.GetType();
            HandleRootComponentRemoveOperation(componentId);
            return HandleRootComponentAddOperationAsync(componentType, newParameters, domElementSelector);
        }
    }

    private void HandleRootComponentRemoveOperation(int componentId)
    {
        _latestRootComponentDirectParameters?.Remove(componentId);
        RemoveRootComponent(componentId);
    }
}
