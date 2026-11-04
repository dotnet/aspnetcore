// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server;

internal sealed partial class ComponentParameterDeserializer
{
    private readonly ILogger<ComponentParameterDeserializer> _logger;
    private readonly ComponentParametersTypeCache _parametersCache;

    public ComponentParameterDeserializer(
        ILogger<ComponentParameterDeserializer> logger,
        ComponentParametersTypeCache parametersCache)
    {
        _logger = logger;
        _parametersCache = parametersCache;
    }

    public bool TryDeserializeParameters(IList<ComponentParameter> parametersDefinitions, IList<object> parameterValues, out ParameterView parameters)
    {
        parameters = default;
        var parametersDictionary = new Dictionary<string, object>();

        if (parameterValues.Count != parametersDefinitions.Count)
        {
            // Mismatched number of definition/parameter values.
            Log.MismatchedParameterAndDefinitions(_logger, parametersDefinitions.Count, parameterValues.Count);
            return false;
        }

        for (var i = 0; i < parametersDefinitions.Count; i++)
        {
            var definition = parametersDefinitions[i];
            if (definition.Name == null)
            {
                Log.MissingParameterDefinitionName(_logger);
                return false;
            }

            if (definition.TypeName == null && definition.Assembly == null)
            {
                parametersDictionary.Add(definition.Name, null);
            }
            else if (definition.TypeName == null || definition.Assembly == null)
            {
                Log.IncompleteParameterDefinition(_logger, definition.Name, definition.TypeName, definition.Assembly);
                return false;
            }
            else if (definition.TypeName == typeof(SerializedRenderFragment).FullName
                && definition.Assembly == "Microsoft.AspNetCore.Components.Endpoints")
            {
                try
                {
                    var value = (JsonElement)parameterValues[i];
                    var serialized = JsonSerializer.Deserialize<SerializedRenderFragment>(
                        value.GetRawText(),
                        ServerComponentSerializationSettings.JsonSerializationOptions);
                    parametersDictionary.Add(definition.Name, RenderFragmentSerializer.Deserialize(serialized!.Nodes, ServerComponentSerializationSettings.JsonSerializationOptions, _parametersCache));
                }
                catch (Exception e)
                {
                    Log.InvalidParameterValue(_logger, definition.Name, definition.TypeName, definition.Assembly, e);
                    return false;
                }
            }
            else
            {
                var parameterType = _parametersCache.GetParameterType(definition.Assembly, definition.TypeName);
                if (parameterType == null)
                {
                    Log.InvalidParameterType(_logger, definition.Name, definition.TypeName, definition.Assembly);
                    return false;
                }
                try
                {
                    object? parameterValue;
                    if (parameterValues[i] is null && IsUnion(parameterType))
                    {
                        // A union whose active case serializes to JSON null (for example a Union(int?, string)
                        // holding a null int?) is still a non-null box, so the prerender protocol records its
                        // type name like any other typed parameter. The value itself serializes to JSON null,
                        // which materializes as a CLR null in the object-typed parameter values array rather than
                        // as a JsonElement. Route the JSON null literal back through the union converter so the
                        // original active case is restored instead of failing the JsonElement cast below.
                        parameterValue = JsonSerializer.Deserialize(
                            "null",
                            parameterType,
                            ServerComponentSerializationSettings.JsonSerializationOptions);
                    }
                    else
                    {
                        // At this point we know the parameter is not null, as we don't serialize the type name or the assembly name
                        // for null parameters.
                        var value = (JsonElement)parameterValues[i];
                        parameterValue = JsonSerializer.Deserialize(
                            value.GetRawText(),
                            parameterType,
                            ServerComponentSerializationSettings.JsonSerializationOptions);
                    }

                    parametersDictionary.Add(definition.Name, parameterValue);
                }
                catch (Exception e)
                {
                    Log.InvalidParameterValue(_logger, definition.Name, definition.TypeName, definition.Assembly, e);
                    return false;
                }
            }
        }

        parameters = ParameterView.FromDictionary(parametersDictionary);
        return true;
    }

    // A C# union is the only typed parameter whose value can legitimately serialize to JSON null while still
    // recording a non-null type name (the box is non-null, but its active case can be a null int? or reference).
    private static bool IsUnion(Type parameterType)
        => ServerComponentSerializationSettings.JsonSerializationOptions
            .GetTypeInfo(parameterType).Kind == JsonTypeInfoKind.Union;

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Parameter values must be an array.", EventName = "ParameterValuesInvalidFormat")]
        internal static partial void ParameterValuesInvalidFormat(ILogger<ComponentParameterDeserializer> logger);

        [LoggerMessage(2, LogLevel.Debug, "The parameter definition for '{ParameterName}' is incomplete: Type='{TypeName}' Assembly='{Assembly}'.", EventName = "IncompleteParameterDefinition")]
        internal static partial void IncompleteParameterDefinition(ILogger<ComponentParameterDeserializer> logger, string parameterName, string typeName, string assembly);

        [LoggerMessage(3, LogLevel.Debug, "The parameter '{ParameterName} with type '{TypeName}' in assembly '{Assembly}' could not be found.", EventName = "InvalidParameterType")]
        internal static partial void InvalidParameterType(ILogger<ComponentParameterDeserializer> logger, string parameterName, string typeName, string assembly);

        [LoggerMessage(4, LogLevel.Debug, "Could not parse the parameter value for parameter '{Name}' of type '{TypeName}' and assembly '{Assembly}'.", EventName = "InvalidParameterValue")]
        internal static partial void InvalidParameterValue(ILogger<ComponentParameterDeserializer> logger, string name, string typeName, string assembly, Exception e);

        [LoggerMessage(5, LogLevel.Debug, "Failed to parse the parameter definitions.", EventName = "FailedToParseParameterDefinitions")]
        internal static partial void FailedToParseParameterDefinitions(ILogger<ComponentParameterDeserializer> logger, Exception e);

        [LoggerMessage(6, LogLevel.Debug, "Failed to parse the parameter values.", EventName = "FailedToParseParameterValues")]
        internal static partial void FailedToParseParameterValues(ILogger<ComponentParameterDeserializer> logger, Exception e);

        [LoggerMessage(7, LogLevel.Debug, "The number of parameter definitions '{DescriptorsLength}' does not match the number parameter values '{ValuesLength}'.", EventName = "MismatchedParameterAndDefinitions")]
        internal static partial void MismatchedParameterAndDefinitions(ILogger<ComponentParameterDeserializer> logger, int descriptorsLength, int valuesLength);

        [LoggerMessage(8, LogLevel.Debug, "The name is missing in a parameter definition.", EventName = "MissingParameterDefinitionName")]
        internal static partial void MissingParameterDefinitionName(ILogger<ComponentParameterDeserializer> logger);
    }
}
