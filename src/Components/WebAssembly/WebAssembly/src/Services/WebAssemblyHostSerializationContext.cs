// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal sealed class WebAssemblyHostSerializationContext
{
    private readonly RootTypeCache _rootComponentCache;
    private readonly WebAssemblyComponentParameterDeserializer _parameterDeserializer;

    public WebAssemblyHostSerializationContext(
        RootTypeCache rootComponentCache,
        IServiceProvider services)
    {
        _rootComponentCache = rootComponentCache;
        var resolver = ComponentJsonMetadata.GetApplicationResolver(services);
        ComponentOptions = WebAssemblyComponentSerializationSettings.CreateOptions(resolver);
        JSInteropOptions = DefaultWebAssemblyJSRuntime.Instance.CreateHostJsonSerializerOptions(resolver);
        _parameterDeserializer = new WebAssemblyComponentParameterDeserializer(
            new ComponentParametersTypeCache(),
            ComponentOptions);
    }

    public JsonSerializerOptions ComponentOptions { get; }

    public JsonSerializerOptions JSInteropOptions { get; }

    public WebRootComponentParameters DeserializeComponentParameters(ComponentMarker marker)
    {
        var definitions = WebAssemblyComponentParameterDeserializer.GetParameterDefinitions(marker.ParameterDefinitions!);
        var values = WebAssemblyComponentParameterDeserializer.GetParameterValues(marker.ParameterValues!);
        var parameters = _parameterDeserializer.DeserializeParameters(definitions, values);

        return new(parameters, definitions, values.AsReadOnly());
    }

    public RootComponentOperationBatch DeserializeOperations(string operationsJson)
    {
        var deserialized = JsonSerializer.Deserialize(
            operationsJson,
            WebAssemblyJsonSerializerContext.Default.RootComponentOperationBatch)!;

        for (var i = 0; i < deserialized.Operations.Length; i++)
        {
            var operation = deserialized.Operations[i];
            if (operation.Type == RootComponentOperationType.Remove)
            {
                continue;
            }

            if (operation.Marker is null)
            {
                throw new InvalidOperationException(
                    $"The component operation of type '{operation.Type}' requires a '{nameof(operation.Marker)}' to be specified.");
            }

            var marker = operation.Marker.Value;
            var componentType = _rootComponentCache.GetRootType(marker.Assembly!, marker.TypeName!)
                ?? throw new InvalidOperationException(
                    $"Root component type '{marker.TypeName}' could not be found in the assembly '{marker.Assembly}'.");
            operation.Descriptor = new(componentType, DeserializeComponentParameters(marker));
        }

        return deserialized;
    }
}
