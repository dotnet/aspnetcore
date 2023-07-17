// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class AutoComponentSerializer
{
    public static void AppendPreamble(TextWriter writer, ServerComponentMarker serverRecord, WebAssemblyComponentMarker webAssemblyRecord)
    {
        var autoRecord = AutoComponentMarker.FromMarkers(serverRecord, webAssemblyRecord);
        var serializedStartRecord = JsonSerializer.Serialize(
            autoRecord,
            ServerComponentSerializationSettings.JsonSerializationOptions);

        writer.Write("<!--Blazor:");
        writer.Write(serializedStartRecord);
        writer.Write("-->");
    }

    public static void AppendEpilogue(TextWriter writer, ServerComponentMarker serverRecord)
    {
        // We always use the server record for the end record.
        ServerComponentSerializer.AppendEpilogue(writer, serverRecord);
    }
}
