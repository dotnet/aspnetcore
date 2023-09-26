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

    // Unlike the equality checking implementation in ParameterView, this method
    // compares the serialized values of parameters. This is because it's a requirement that
    // component parameters for interactive root components are serializable, so we can
    // compare the serialized representation without relying on .NET equality checking
    // for the deserialized values, which may yield false negatives.
    public bool DefinitelyEquals(in WebRootComponentParameters other)
    {
        var count = _parameterDefinitions.Count;
        if (count != other._parameterDefinitions.Count)
        {
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            // We rely on parameter definitions and values having a consistent order between
            // multiple endpoint invocations. This should be true because component parameters
            // are usually rendered in a deterministic order.
            var definition = _parameterDefinitions[i];
            var otherDefinition = other._parameterDefinitions[i];
            if (!string.Equals(definition.Name, otherDefinition.Name, StringComparison.Ordinal) ||
                !string.Equals(definition.TypeName, otherDefinition.TypeName, StringComparison.Ordinal) ||
                !string.Equals(definition.Assembly, otherDefinition.Assembly, StringComparison.Ordinal))
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
