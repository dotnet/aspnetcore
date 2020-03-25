// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Server
{
    // **Component descriptor protocol**
    // MVC serializes one or more components as comments in HTML.
    // Each comment is in the form <!-- Blazor:<<Json>>--> for example { "type": "server", "sequence": 0, descriptor: "base64(dataprotected(<<ServerComponent>>))" }
    // Where <<Json>> has the following properties:
    // 'type' indicates the marker type. For now it's limited to server.
    // 'sequence' indicates the order in which this component got rendered on the server.
    // 'descriptor' a data-protected payload that allows the server to validate the legitimacy of the rendered component.
    // 'prerenderId' a unique identifier that uniquely identifies the marker to match start and end markers.
    //
    // descriptor holds the information to validate a component render request. It prevents an infinite number of components
    // from being rendered by a given client.
    //
    // descriptor is a data protected json payload that holds the following information
    // 'sequence' indicates the order in which this component got rendered on the server.
    // 'assemblyName' the assembly name for the rendered component.
    // 'type' the full type name for the rendered component.
    // 'parameterDefinitions' a JSON serialized array that contains the definitions for the parameters including their names and types and assemblies.
    // 'parameterValues' a JSON serialized array containing the parameter values.
    // 'invocationId' a random string that matches all components rendered by as part of a single HTTP response.
    // For example: base64(dataprotection({ "sequence": 1, "assemblyName": "Microsoft.AspNetCore.Components", "type":"Microsoft.AspNetCore.Components.Routing.Router", "invocationId": "<<guid>>"}))
    // With parameters
    // For example: base64(dataprotection({ "sequence": 1, "assemblyName": "Microsoft.AspNetCore.Components", "type":"Microsoft.AspNetCore.Components.Routing.Router", "invocationId": "<<guid>>", parameterDefinitions: "[{ \"name\":\"Parameter\", \"typeName\":\"string\", \"assembly\":\"System.Private.CoreLib\"}], parameterValues: [<<string-value>>]}))

    // Serialization:
    // For a given response, MVC renders one or more markers in sequence, including a descriptor for each rendered
    // component containing the information described above.

    // Deserialization:
    // To prevent a client from rendering an infinite amount of components, we require clients to send all component
    // markers in order. They can do so thanks to the sequence included in the marker.
    // When we process a marker we do the following.
    // * We unprotect the data-protected information.
    // * We validate that the sequence number for the descriptor goes after the previous descriptor.
    // * We compare the invocationId for the previous descriptor against the invocationId for the current descriptor to make sure they match.
    // By doing this we achieve three things:
    // * We ensure that the descriptor came from the server.
    // * We ensure that a client can't just send an infinite amount of components to render.
    // * We ensure that we do the minimal amount of work in the case of an invalid sequence of descriptors.
    //
    // For example:
    // A client can't just send 100 component markers and force us to process them if the server didn't generate those 100 markers.
    //  * If a marker is out of sequence we will fail early, so we process at most n-1 markers.
    //  * If a marker has the right sequence but the invocation ID is different we will fail at that point. We know for sure that the
    //    component wasn't render as part of the same response.
    //  * If a marker can't be unprotected we will fail early. We know that the marker was tampered with and can't be trusted.
    internal class ServerComponentDeserializer
    {
        private readonly IDataProtector _dataProtector;
        private readonly ILogger<ServerComponentDeserializer> _logger;
        private readonly ServerComponentTypeCache _rootComponentTypeCache;
        private readonly ComponentParameterDeserializer _parametersDeserializer;

        public ServerComponentDeserializer(
            IDataProtectionProvider dataProtectionProvider,
            ILogger<ServerComponentDeserializer> logger,
            ServerComponentTypeCache rootComponentTypeCache,
            ComponentParameterDeserializer parametersDeserializer)
        {
            // When we protect the data we use a time-limited data protector with the
            // limits established in 'ServerComponentSerializationSettings.DataExpiration'
            // We don't use any of the additional methods provided by ITimeLimitedDataProtector
            // in this class, but we need to create one for the unprotect operations to work
            // even though we simply call '_dataProtector.Unprotect'.
            // See the comment in ServerComponentSerializationSettings.DataExpiration to understand
            // why we limit the validity of the protected payloads.
            _dataProtector = dataProtectionProvider
                .CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
                .ToTimeLimitedDataProtector();

            _logger = logger;
            _rootComponentTypeCache = rootComponentTypeCache;
            _parametersDeserializer = parametersDeserializer;
        }

        public bool TryDeserializeComponentDescriptorCollection(string serializedComponentRecords, out List<ComponentDescriptor> descriptors)
        {
            var markers = JsonSerializer.Deserialize<IEnumerable<ServerComponentMarker>>(serializedComponentRecords, ServerComponentSerializationSettings.JsonSerializationOptions);
            descriptors = new List<ComponentDescriptor>();
            int lastSequence = -1;

            var previousInstance = new ServerComponent();
            foreach (var marker in markers)
            {
                if (marker.Type != ServerComponentMarker.ServerMarkerType)
                {
                    Log.InvalidMarkerType(_logger, marker.Type);
                    descriptors.Clear();
                    return false;
                }

                if (marker.Descriptor == null)
                {
                    Log.MissingMarkerDescriptor(_logger);
                    descriptors.Clear();
                    return false;
                }

                var (descriptor, serverComponent) = DeserializeServerComponent(marker);
                if (descriptor == null)
                {
                    // We failed to deserialize the component descriptor for some reason.
                    descriptors.Clear();
                    return false;
                }

                // We force our client to send the descriptors in order so that we do minimal work.
                // The list of descriptors starts with 0 and lastSequence is initialized to -1 so this
                // check covers that the sequence starts by 0.
                if (lastSequence != serverComponent.Sequence - 1)
                {
                    if (lastSequence == -1)
                    {
                        Log.DescriptorSequenceMustStartAtZero(_logger, serverComponent.Sequence);
                    }
                    else
                    {
                        Log.OutOfSequenceDescriptor(_logger, lastSequence, serverComponent.Sequence);
                    }
                    descriptors.Clear();
                    return false;
                }

                if (lastSequence != -1 && !previousInstance.InvocationId.Equals(serverComponent.InvocationId))
                {
                    Log.MismatchedInvocationId(_logger, previousInstance.InvocationId.ToString("N"), serverComponent.InvocationId.ToString("N"));
                    descriptors.Clear();
                    return false;
                }

                // As described below, we build a chain of descriptors to prevent being flooded by
                // descriptors from a client not behaving properly.
                lastSequence = serverComponent.Sequence;
                previousInstance = serverComponent;
                descriptors.Add(descriptor);
            }

            return true;
        }

        private (ComponentDescriptor, ServerComponent) DeserializeServerComponent(ServerComponentMarker record)
        {
            string unprotected;
            try
            {
                var payload = Convert.FromBase64String(record.Descriptor);
                var unprotectedBytes = _dataProtector.Unprotect(payload);
                unprotected = Encoding.UTF8.GetString(unprotectedBytes);
            }
            catch (Exception e)
            {
                Log.FailedToUnprotectDescriptor(_logger, e);
                return default;
            }

            ServerComponent serverComponent;
            try
            {
                serverComponent = JsonSerializer.Deserialize<ServerComponent>(
                    unprotected,
                    ServerComponentSerializationSettings.JsonSerializationOptions);
            }
            catch (Exception e)
            {
                Log.FailedToDeserializeDescriptor(_logger, e);
                return default;
            }

            var componentType = _rootComponentTypeCache
                .GetRootComponent(serverComponent.AssemblyName, serverComponent.TypeName);

            if (componentType == null)
            {
                Log.FailedToFindComponent(_logger, serverComponent.TypeName, serverComponent.AssemblyName);
                return default;
            }

            if (!_parametersDeserializer.TryDeserializeParameters(serverComponent.ParameterDefinitions, serverComponent.ParameterValues, out var parameters))
            {
                // TryDeserializeParameters does appropriate logging.
                return default;
            }
            
            var componentDescriptor = new ComponentDescriptor
            {
                ComponentType = componentType,
                Parameters = parameters,
                Sequence = serverComponent.Sequence
            };

            return (componentDescriptor, serverComponent);
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _failedToDeserializeDescriptor =
                LoggerMessage.Define(
                    LogLevel.Debug,
                    new EventId(1, "FailedToDeserializeDescriptor"),
                    "Failed to deserialize the component descriptor.");

            private static readonly Action<ILogger, string, string, Exception> _failedToFindComponent =
                LoggerMessage.Define<string, string>(
                    LogLevel.Debug,
                    new EventId(2, "FailedToFindComponent"),
                    "Failed to find component '{ComponentName}' in assembly '{Assembly}'.");

            private static readonly Action<ILogger, Exception> _failedToUnprotectDescriptor =
                LoggerMessage.Define(
                    LogLevel.Debug,
                    new EventId(3, "FailedToUnprotectDescriptor"),
                    "Failed to unprotect the component descriptor.");

            private static readonly Action<ILogger, string, Exception> _invalidMarkerType =
                LoggerMessage.Define<string>(
                    LogLevel.Debug,
                    new EventId(4, "InvalidMarkerType"),
                    "Invalid component marker type '{}'.");

            private static readonly Action<ILogger, Exception> _missingMarkerDescriptor =
                LoggerMessage.Define(
                    LogLevel.Debug,
                    new EventId(5, "MissingMarkerDescriptor"),
                    "The component marker is missing the descriptor.");

            private static readonly Action<ILogger, string, string, Exception> _mismatchedInvocationId =
                LoggerMessage.Define<string, string>(
                    LogLevel.Debug,
                    new EventId(6, "MismatchedInvocationId"),
                    "The descriptor invocationId is '{invocationId}' and got a descriptor with invocationId '{currentInvocationId}'.");

            private static readonly Action<ILogger, int, int, Exception> _outOfSequenceDescriptor =
                LoggerMessage.Define<int, int>(
                    LogLevel.Debug,
                    new EventId(7, "OutOfSequenceDescriptor"),
                    "The last descriptor sequence was '{lastSequence}' and got a descriptor with sequence '{receivedSequence}'.");

            private static readonly Action<ILogger, int, Exception> _descriptorSequenceMustStartAtZero =
                LoggerMessage.Define<int>(
                    LogLevel.Debug,
                    new EventId(8, "DescriptorSequenceMustStartAtZero"),
                    "The descriptor sequence '{sequence}' is an invalid start sequence.");

            public static void FailedToDeserializeDescriptor(ILogger<ServerComponentDeserializer> logger, Exception e) =>
                _failedToDeserializeDescriptor(logger, e);

            public static void FailedToFindComponent(ILogger<ServerComponentDeserializer> logger, string assemblyName, string typeName) =>
                _failedToFindComponent(logger, assemblyName, typeName, null);

            public static void FailedToUnprotectDescriptor(ILogger<ServerComponentDeserializer> logger, Exception e) =>
                _failedToUnprotectDescriptor(logger, e);

            public static void InvalidMarkerType(ILogger<ServerComponentDeserializer> logger, string markerType) =>
                _invalidMarkerType(logger, markerType, null);

            public static void MissingMarkerDescriptor(ILogger<ServerComponentDeserializer> logger) =>
                _missingMarkerDescriptor(logger, null);

            public static void MismatchedInvocationId(ILogger<ServerComponentDeserializer> logger, string invocationId, string currentInvocationId) =>
                _mismatchedInvocationId(logger, invocationId, currentInvocationId, null);

            public static void OutOfSequenceDescriptor(ILogger<ServerComponentDeserializer> logger, int lastSequence, int sequence) =>
                _outOfSequenceDescriptor(logger, lastSequence, sequence, null);

            public static void DescriptorSequenceMustStartAtZero(ILogger<ServerComponentDeserializer> logger, int sequence) =>
                _descriptorSequenceMustStartAtZero(logger, sequence, null);
        }
    }
}
