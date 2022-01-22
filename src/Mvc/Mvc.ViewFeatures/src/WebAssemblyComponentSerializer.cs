// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

// See the details of the component serialization protocol in WebAssemblyComponentDeserializer.cs on the Components solution.
internal class WebAssemblyComponentSerializer
{
    public static WebAssemblyComponentMarker SerializeInvocation(Type type, ParameterView parameters, bool prerendered)
    {
        var assembly = type.Assembly.GetName().Name;
        var typeFullName = type.FullName;
        var (definitions, values) = ComponentParameter.FromParameterView(parameters);

        // We need to serialize and Base64 encode parameters separately since they can contain arbitrary data that might
        // cause the HTML comment to be invalid (like if you serialize a string that contains two consecutive dashes "--").
        var serializedDefinitions = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(definitions, WebAssemblyComponentSerializationSettings.JsonSerializationOptions));
        var serializedValues = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(values, WebAssemblyComponentSerializationSettings.JsonSerializationOptions));

        return prerendered ? WebAssemblyComponentMarker.Prerendered(assembly, typeFullName, serializedDefinitions, serializedValues) :
            WebAssemblyComponentMarker.NonPrerendered(assembly, typeFullName, serializedDefinitions, serializedValues);
    }

    internal static void AppendPreamble(ViewBuffer viewBuffer, WebAssemblyComponentMarker record)
    {
        var serializedStartRecord = JsonSerializer.Serialize(
            record,
            WebAssemblyComponentSerializationSettings.JsonSerializationOptions);

        viewBuffer.AppendHtml("<!--Blazor:");
        viewBuffer.AppendHtml(serializedStartRecord);
        viewBuffer.AppendHtml("-->");
    }

    internal static void AppendEpilogue(ViewBuffer viewBuffer, WebAssemblyComponentMarker record)
    {
        var endRecord = JsonSerializer.Serialize(
            record.GetEndRecord(),
            WebAssemblyComponentSerializationSettings.JsonSerializationOptions);

        viewBuffer.AppendHtml("<!--Blazor:");
        viewBuffer.AppendHtml(endRecord);
        viewBuffer.AppendHtml("-->");
    }
}
