// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

// See the details of the component serialization protocol in ServerComponentDeserializer.cs on the Components solution.
internal class ServerComponentSerializer
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
            rootComponent.Assembly.GetName().Name,
            rootComponent.FullName,
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
    internal static void AppendPreamble(IHtmlContentBuilder htmlContentBuilder, ServerComponentMarker record)
    {
        var serializedStartRecord = JsonSerializer.Serialize(
            record,
            ServerComponentSerializationSettings.JsonSerializationOptions);

        htmlContentBuilder.AppendHtml("<!--Blazor:");
        htmlContentBuilder.AppendHtml(serializedStartRecord);
        htmlContentBuilder.AppendHtml("-->");
    }

    internal static void AppendEpilogue(IHtmlContentBuilder htmlContentBuilder, ServerComponentMarker record)
    {
        var endRecord = JsonSerializer.Serialize(
            record.GetEndRecord(),
            ServerComponentSerializationSettings.JsonSerializationOptions);

        htmlContentBuilder.AppendHtml("<!--Blazor:");
        htmlContentBuilder.AppendHtml(endRecord);
        htmlContentBuilder.AppendHtml("-->");
    }
}
