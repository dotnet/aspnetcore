// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Components.Endpoints;

// See the details of the component serialization protocol in ServerComponentDeserializer.cs on the Components solution.
internal sealed class ServerComponentSerializer
{
    private readonly ITimeLimitedDataProtector _dataProtector;

    public ServerComponentSerializer(IDataProtectionProvider dataProtectionProvider) =>
        _dataProtector = dataProtectionProvider
            .CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

    public void SerializeInvocation(ref ComponentMarker marker, ServerComponentInvocationSequence invocationId, Type type, ParameterView parameters)
    {
        var (sequence, serverComponent) = CreateSerializedServerComponent(invocationId, type, parameters, marker.Key);
        marker.WriteServerData(sequence, serverComponent);
    }

    private (int sequence, string payload) CreateSerializedServerComponent(
        ServerComponentInvocationSequence invocationId,
        Type rootComponent,
        ParameterView parameters,
        ComponentMarkerKey? key)
    {
        var sequence = invocationId.Next();

        var (definitions, values) = ComponentParameter.FromParameterView(parameters);

        var serverComponent = new ServerComponent(
            sequence,
            key,
            rootComponent.Assembly.GetName().Name ?? throw new InvalidOperationException("Cannot prerender components from assemblies with a null name"),
            rootComponent.FullName ?? throw new InvalidOperationException("Cannot prerender component types with a null name"),
            definitions,
            values,
            invocationId.Value);

        var serializedServerComponentBytes = JsonSerializer.SerializeToUtf8Bytes(serverComponent, ServerComponentSerializationSettings.JsonSerializationOptions);
        var protectedBytes = _dataProtector.Protect(serializedServerComponentBytes, ServerComponentSerializationSettings.DataExpiration);
        return (serverComponent.Sequence, Convert.ToBase64String(protectedBytes));
    }
}
