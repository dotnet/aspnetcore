// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    // See the details of the component serialization protocol in ServerComponentDeserializer.cs on the Components solution.
    internal class ServerComponentSerializer
    {
        private readonly ITimeLimitedDataProtector _dataProtector;

        public ServerComponentSerializer(IDataProtectionProvider dataProtectionProvider) =>
            _dataProtector = dataProtectionProvider
                .CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
                .ToTimeLimitedDataProtector();

        public ServerComponentMarker SerializeInvocation(ServerComponentInvocationSequence invocationId, Type type, ParameterView parameters, bool prerendered)
        {
            var (sequence, serverComponent) = CreateSerializedServerComponent(invocationId, type, parameters);
            return prerendered ? ServerComponentMarker.Prerendered(sequence, serverComponent) : ServerComponentMarker.NonPrerendered(sequence, serverComponent);
        }

        private (int sequence, string payload) CreateSerializedServerComponent(
            ServerComponentInvocationSequence invocationId,
            Type rootComponent,
            ParameterView parameters)
        {
            var sequence = invocationId.Next();

            var (definitions, values) = ComponentParameter.FromParameterView(parameters);

            var serverComponent = new ServerComponent(
                sequence,
                rootComponent.Assembly.GetName().Name,
                rootComponent.FullName,
                definitions,
                values,
                invocationId.Value);

            var serializedServerComponentBytes = JsonSerializer.SerializeToUtf8Bytes(serverComponent, ServerComponentSerializationSettings.JsonSerializationOptions);
            var protectedBytes = _dataProtector.Protect(serializedServerComponentBytes, ServerComponentSerializationSettings.DataExpiration);
            return (serverComponent.Sequence, Convert.ToBase64String(protectedBytes));
        }

        internal IEnumerable<string> GetPreamble(ServerComponentMarker record)
        {
            var serializedStartRecord = JsonSerializer.Serialize(
                record,
                ServerComponentSerializationSettings.JsonSerializationOptions);

            if (record.PrerenderId != null)
            {
                return PrerenderedStart(serializedStartRecord);
            }
            else
            {
                return NonPrerenderedSequence(serializedStartRecord);
            }

            static IEnumerable<string> PrerenderedStart(string startRecord)
            {
                yield return "<!--Blazor:";
                yield return startRecord;
                yield return "-->";
            }

            static IEnumerable<string> NonPrerenderedSequence(string record)
            {
                yield return "<!--Blazor:";
                yield return record;
                yield return "-->";
            }
        }

        internal IEnumerable<string> GetEpilogue(ServerComponentMarker record)
        {
            var serializedStartRecord = JsonSerializer.Serialize(
                record.GetEndRecord(),
                ServerComponentSerializationSettings.JsonSerializationOptions);

            return PrerenderEnd(serializedStartRecord);

            static IEnumerable<string> PrerenderEnd(string endRecord)
            {
                yield return "<!--Blazor:";
                yield return endRecord;
                yield return "-->";
            }
        }
    }
}
