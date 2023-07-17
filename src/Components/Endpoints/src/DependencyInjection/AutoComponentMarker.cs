// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

internal struct AutoComponentMarker
{
    public const string AutoMarkerType = "auto";

    public string Type { get; set; }

    public string? PrerenderId { get; set; }

    public ServerComponentMarker Server { get; set; }

    public WebAssemblyComponentMarker WebAssembly { get; set; }

    public static AutoComponentMarker FromMarkers(ServerComponentMarker server, WebAssemblyComponentMarker webAssembly) =>
        new() { Type = AutoMarkerType, PrerenderId = server.PrerenderId, Server = server, WebAssembly = webAssembly };
}
