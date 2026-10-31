// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Test.Helpers;

public static class IComponentExtensions
{
    public static void SetParameters(
        this IComponent component,
        Dictionary<string, object> parameters)
    {
        component.SetParametersAsync(ParameterView.FromDictionary(parameters));
    }
}
