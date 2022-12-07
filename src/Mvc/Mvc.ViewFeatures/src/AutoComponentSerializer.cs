// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

// TODO: We should merge this and ServerComponentSerializer and WebAssemblyComponentSerializer into a single type
// since they are all almost the same already. We don't need completely independent implementations of them or separate
// definitions of their payload format, either on the .NET side or on the TypeScript side.
internal static class AutoComponentSerializer
{
    internal static void AppendPreamble(IHtmlContentBuilder htmlContentBuilder, ServerComponentMarker serverRecord, WebAssemblyComponentMarker webAssemblyRecord)
    {
        var serializedStartRecord = JsonSerializer.Serialize(
            new { type = "auto", server = serverRecord, webAssembly = webAssemblyRecord },
            ServerComponentSerializationSettings.JsonSerializationOptions);

        htmlContentBuilder.AppendHtml("<!--Blazor:");
        htmlContentBuilder.AppendHtml(serializedStartRecord);
        htmlContentBuilder.AppendHtml("-->");
    }

    internal static void AppendEpilogue(IHtmlContentBuilder htmlContentBuilder, ServerComponentMarker serverRecord, WebAssemblyComponentMarker webAssemblyRecord)
    {
        var endRecord = JsonSerializer.Serialize(
            new { type = "auto", server = serverRecord.GetEndRecord(), webAssembly = webAssemblyRecord.GetEndRecord() },
            ServerComponentSerializationSettings.JsonSerializationOptions);

        htmlContentBuilder.AppendHtml("<!--Blazor:");
        htmlContentBuilder.AppendHtml(endRecord);
        htmlContentBuilder.AppendHtml("-->");
    }
}
