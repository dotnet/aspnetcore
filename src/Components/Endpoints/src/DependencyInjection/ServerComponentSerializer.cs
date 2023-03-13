// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Components.Endpoints;

// See the details of the component serialization protocol in ServerComponentDeserializer.cs on the Components solution.
internal sealed class ServerComponentSerializer
{
    public const int PreambleBufferSize = 3;

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
            rootComponent.Assembly.GetName().Name ?? throw new InvalidOperationException("Cannot prerender components from assemblies with a null name"),
            rootComponent.FullName ?? throw new InvalidOperationException("Cannot prerender component types with a null name"),
            definitions,
            values,
            invocationId.Value);

        var serializedServerComponentBytes = JsonSerializer.SerializeToUtf8Bytes(serverComponent, ServerComponentSerializationSettings.JsonSerializationOptions);
        var protectedBytes = _dataProtector.Protect(serializedServerComponentBytes, ServerComponentSerializationSettings.DataExpiration);
        return (serverComponent.Sequence, Convert.ToBase64String(protectedBytes));
    }

    /// <remarks>
    /// Remember to update <see cref="PreambleBufferSize"/> if the number of entries being appended in this function changes.
    /// </remarks>
    internal static void AppendPreamble(TextWriter writer, ServerComponentMarker record)
    {
        var serializedStartRecord = JsonSerializer.Serialize(
            record,
            ServerComponentSerializationSettings.JsonSerializationOptions);

        writer.Write("<!--Blazor:");
        writer.Write(serializedStartRecord);
        writer.Write("-->");
    }

    internal static void AppendEpilogue(TextWriter writer, ServerComponentMarker record)
    {
        var endRecord = JsonSerializer.Serialize(
            record.GetEndRecord(),
            ServerComponentSerializationSettings.JsonSerializationOptions);

        writer.Write("<!--Blazor:");
        writer.Write(endRecord);
        writer.Write("-->");
    }
}
