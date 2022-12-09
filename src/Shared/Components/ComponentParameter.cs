// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components;

internal struct ComponentParameter
{
    public string Name { get; set; }
    public string? TypeName { get; set; }
    public string? Assembly { get; set; }

    public static (IList<ComponentParameter> parameterDefinitions, IList<object?> parameterValues) FromParameterView(ParameterView parameters, Func<RenderFragment, string>? renderFragmentSerializer)
    {
        var parameterDefinitions = new List<ComponentParameter>();
        var parameterValues = new List<object?>();
        foreach (var kvp in parameters)
        {
            var valueType = kvp.Value?.GetType();
            parameterDefinitions.Add(new ComponentParameter
            {
                Name = kvp.Name,
                TypeName = valueType?.FullName,
                Assembly = valueType?.Assembly?.GetName()?.Name
            });

            var value = kvp.Value is RenderFragment fragment && renderFragmentSerializer is not null
                ? renderFragmentSerializer(fragment)
                : kvp.Value;
            parameterValues.Add(value);
        }

        return (parameterDefinitions, parameterValues);
    }
}
