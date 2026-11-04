// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

#nullable enable // This is shared-source with Mvc.ViewFeatures which does not enable nullability by default

internal struct ComponentParameter
{
    public string Name { get; set; }
    public string? TypeName { get; set; }
    public string? Assembly { get; set; }

    public static (IList<ComponentParameter> parameterDefinitions, IList<object?> parameterValues) FromParameterView(ParameterView parameters)
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

            parameterValues.Add(kvp.Value);
        }

        return (parameterDefinitions, parameterValues);
    }
}
