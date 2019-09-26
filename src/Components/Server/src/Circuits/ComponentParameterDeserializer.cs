// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server
{
    internal class ComponentParameterDeserializer
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
                else
                {
                    var parameterType = _parametersCache.GetParameterType(definition.Assembly, definition.TypeName);
                    if (parameterType == null)
                    {
                        Log.InvalidParameterType(_logger, definition.Name, definition.Assembly, definition.TypeName);
                        return false;
                    }
                    try
                    {
                        // At this point we know the parameter is not null, as we don't serialize the type name or the assembly name
                        // for null parameters.
                        var value = (JsonElement)parameterValues[i];
                        var parameterValue = JsonSerializer.Deserialize(
                            value.GetRawText(),
                            parameterType,
                            ServerComponentSerializationSettings.JsonSerializationOptions);

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

        private ComponentParameter[] GetParameterDefinitions(string parametersDefinitions)
        {
            try
            {
                return JsonSerializer.Deserialize<ComponentParameter[]>(parametersDefinitions, ServerComponentSerializationSettings.JsonSerializationOptions);
            }
            catch (Exception e)
            {
                Log.FailedToParseParameterDefinitions(_logger, e);
                return null;
            }
        }

        private JsonDocument GetParameterValues(string parameterValues)
        {
            try
            {
                return JsonDocument.Parse(parameterValues);
            }
            catch (Exception e)
            {
                Log.FailedToParseParameterValues(_logger, e);
                return null;
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _parameterValuesInvalidFormat =
                LoggerMessage.Define(
                    LogLevel.Debug,
                    new EventId(1, "ParameterValuesInvalidFormat"),
                    "Parameter values must be an array.");

            private static readonly Action<ILogger, string, string, string, Exception> _incompleteParameterDefinition =
                LoggerMessage.Define<string, string, string>(
                    LogLevel.Debug,
                    new EventId(2, "IncompleteParameterDefinition"),
                    "The parameter definition for '{ParameterName}' is incomplete: Type='{TypeName}' Assembly='{Assembly}'.");

            private static readonly Action<ILogger, string, string, string, Exception> _invalidParameterType =
                LoggerMessage.Define<string, string, string>(
                    LogLevel.Debug,
                    new EventId(3, "InvalidParameterType"),
                    "The parameter '{ParameterName} with type '{TypeName}' in assembly '{Assembly}' could not be found.");

            private static readonly Action<ILogger, string, string, string, Exception> _invalidParameterValue =
                LoggerMessage.Define<string, string, string>(
                    LogLevel.Debug,
                    new EventId(4, "InvalidParameterValue"),
                    "Could not parse the parameter value for parameter '{Name}' of type '{TypeName}' and assembly '{Assembly}'.");

            private static readonly Action<ILogger, Exception> _failedToParseParameterDefinitions =
                LoggerMessage.Define(
                    LogLevel.Debug,
                    new EventId(5, "FailedToParseParameterDefinitions"),
                    "Failed to parse the parameter definitions.");

            private static readonly Action<ILogger, Exception> _failedToParseParameterValues =
                LoggerMessage.Define(
                    LogLevel.Debug,
                    new EventId(6, "FailedToParseParameterValues"),
                    "Failed to parse the parameter values.");

            private static readonly Action<ILogger, int, int, Exception> _mismatchedParameterAndDefinitions =
                LoggerMessage.Define<int, int>(
                    LogLevel.Debug,
                    new EventId(7, "MismatchedParameterAndDefinitions"),
                    "The number of parameter definitions '{DescriptorsLength}' does not match the number parameter values '{ValuesLength}'.");

            private static readonly Action<ILogger, Exception> _missingParameterDefinitionName =
                LoggerMessage.Define(
                    LogLevel.Debug,
                    new EventId(8, "MissingParameterDefinitionName"),
                    "The name is missing in a parameter definition.");

            internal static void ParameterValuesInvalidFormat(ILogger<ComponentParameterDeserializer> logger) =>
                _parameterValuesInvalidFormat(logger, null);

            internal static void IncompleteParameterDefinition(ILogger<ComponentParameterDeserializer> logger, string name, string typeName, string assembly) =>
                _incompleteParameterDefinition(logger, name, typeName, assembly, null);

            internal static void InvalidParameterType(ILogger<ComponentParameterDeserializer> logger, string name, string assembly, string typeName) =>
                _invalidParameterType(logger, name, assembly, typeName, null);

            internal static void InvalidParameterValue(ILogger<ComponentParameterDeserializer> logger, string name, string typeName, string assembly, Exception e) =>
                _invalidParameterValue(logger, name, typeName, assembly,e);

            internal static void FailedToParseParameterDefinitions(ILogger<ComponentParameterDeserializer> logger, Exception e) =>
                _failedToParseParameterDefinitions(logger, e);

            internal static void FailedToParseParameterValues(ILogger<ComponentParameterDeserializer> logger, Exception e) =>
                _failedToParseParameterValues(logger, e);

            internal static void MismatchedParameterAndDefinitions(ILogger<ComponentParameterDeserializer> logger, int definitionsLength, int valuesLength) =>
                _mismatchedParameterAndDefinitions(logger, definitionsLength, valuesLength, null);

            internal static void MissingParameterDefinitionName(ILogger<ComponentParameterDeserializer> logger) =>
                _missingParameterDefinitionName(logger, null);
        }
    }
}
