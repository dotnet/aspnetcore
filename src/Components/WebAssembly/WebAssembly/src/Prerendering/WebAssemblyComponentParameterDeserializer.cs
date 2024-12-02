// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

internal sealed class WebAssemblyComponentParameterDeserializer
{
    private readonly ComponentParametersTypeCache _parametersCache;

    public WebAssemblyComponentParameterDeserializer(
        ComponentParametersTypeCache parametersCache)
    {
        _parametersCache = parametersCache;
    }

    public static WebAssemblyComponentParameterDeserializer Instance { get; } = new WebAssemblyComponentParameterDeserializer(new ComponentParametersTypeCache());

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect application code is configured to preserve component parameter types.")]
    public ParameterView DeserializeParameters(IList<ComponentParameter> parametersDefinitions, IList<object> parameterValues)
    {
        var parametersDictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (parameterValues.Count != parametersDefinitions.Count)
        {
            // Mismatched number of definition/parameter values.
            throw new InvalidOperationException($"The number of parameter definitions '{parametersDefinitions.Count}' does not match the number parameter values '{parameterValues.Count}'.");
        }

        for (var i = 0; i < parametersDefinitions.Count; i++)
        {
            var definition = parametersDefinitions[i];
            if (definition.Name == null)
            {
                throw new InvalidOperationException("The name is missing in a parameter definition.");
            }

            if (definition.TypeName == null && definition.Assembly == null)
            {
                parametersDictionary[definition.Name] = null;
            }
            else if (definition.TypeName == null || definition.Assembly == null)
            {
                throw new InvalidOperationException($"The parameter definition for '{definition.Name}' is incomplete: Type='{definition.TypeName}' Assembly='{definition.Assembly}'.");
            }
            else
            {
                var parameterType = _parametersCache.GetParameterType(definition.Assembly, definition.TypeName);
                if (parameterType == null)
                {
                    throw new InvalidOperationException($"The parameter '{definition.Name}' with type '{definition.TypeName}' in assembly '{definition.Assembly}' could not be found.");
                }
                try
                {
                    var value = (JsonElement)parameterValues[i];
                    var parameterValue = JsonSerializer.Deserialize(
                        value.GetRawText(),
                        parameterType,
                        WebAssemblyComponentSerializationSettings.JsonSerializationOptions);

                    parametersDictionary[definition.Name] = parameterValue;
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Could not parse the parameter value for parameter '{definition.Name}' of type '{definition.TypeName}' and assembly '{definition.Assembly}'.", e);
                }
            }
        }

        return ParameterView.FromDictionary(parametersDictionary);
    }

    [DynamicDependency(JsonSerialized, typeof(ComponentParameter))]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The correct members will be preserved by the above DynamicDependency.")]
    public static ComponentParameter[] GetParameterDefinitions(string parametersDefinitions)
    {
        return JsonSerializer.Deserialize(parametersDefinitions, WebAssemblyJsonSerializerContext.Default.ComponentParameterArray)!;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect application code is configured to preserve component parameter types.")]
    public static IList<object> GetParameterValues(string parameterValues)
    {
        return JsonSerializer.Deserialize(parameterValues, WebAssemblyJsonSerializerContext.Default.IListObject)!;
    }
}
