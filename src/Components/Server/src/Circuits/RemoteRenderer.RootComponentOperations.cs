// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server.Circuits;

// Contains logic for root components that get dynamically updated in Blazor Web scenarios.
internal partial class RemoteRenderer
{
    private Dictionary<int, ParameterView>? _latestRootComponentDirectParameters;

    internal Task HandleRootComponentAddOperationAsync(Type componentType, ParameterView parameters, string domElementSelector)
    {
        var componentId = AddRootComponent(componentType, domElementSelector);

        _latestRootComponentDirectParameters ??= new();
        _latestRootComponentDirectParameters[componentId] = parameters;

        return RenderRootComponentAsync(componentId, parameters);
    }

    internal Task HandleRootComponentRemoveOperationAsync(int componentId, Type componentType, ParameterView newParameters, string domElementSelector)
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
            // it and replace it with a new one. This is because it's the client's choice how to
            // match prerendered components with existing components, and we don't want to allow
            // clients to maliciously assign parameters to the wrong component.
            HandleRootComponentRemoveOperation(componentId);
            return HandleRootComponentAddOperationAsync(componentType, newParameters, domElementSelector);
        }
    }

    internal void HandleRootComponentRemoveOperation(int componentId)
    {
        _latestRootComponentDirectParameters?.Remove(componentId);
        RemoveRootComponent(componentId);
    }
}
