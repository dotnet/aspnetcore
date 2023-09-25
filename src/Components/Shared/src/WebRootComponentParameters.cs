// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components;

internal readonly struct WebRootComponentParameters(
    ParameterView parameterView,
    IReadOnlyList<ComponentParameter> parameterDefinitions,
    IReadOnlyList<object> serializedParameterValues)
{
    public static readonly WebRootComponentParameters Empty = new(ParameterView.Empty, [], []);

    // The parameter definitions and values are assumed to be the same length.
    private readonly IReadOnlyList<ComponentParameter> _parameterDefinitions = parameterDefinitions;
    private readonly IReadOnlyList<object> _serializedParameterValues = serializedParameterValues;

    public ParameterView Parameters => parameterView;

    public bool DefinitelyEquals(in WebRootComponentParameters other)
    {
        var count = _parameterDefinitions.Count;
        if (count != other._parameterDefinitions.Count)
        {
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            if (!string.Equals(_parameterDefinitions[i].Name, other._parameterDefinitions[i].Name, StringComparison.Ordinal))
            {
                return false;
            }

            var value = ((JsonElement)_serializedParameterValues[i]).GetRawText();
            var otherValue = ((JsonElement)other._serializedParameterValues[i]).GetRawText();
            if (!string.Equals(value, otherValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
