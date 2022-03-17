// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.AspNetCore.Components;

internal struct WebAssemblyComponentMarker
{
    public const string ClientMarkerType = "webassembly";

    public WebAssemblyComponentMarker(string type, string assembly, string typeName, string parameterDefinitions, string parameterValues, string? prerenderId) =>
        (Type, Assembly, TypeName, ParameterDefinitions, ParameterValues, PrerenderId) = (type, assembly, typeName, parameterDefinitions, parameterValues, prerenderId);

    public string Type { get; set; }

    public string Assembly { get; set; }

    public string TypeName { get; set; }

    public string ParameterDefinitions { get; set; }

    public string ParameterValues { get; set; }

    public string? PrerenderId { get; set; }

    internal static WebAssemblyComponentMarker NonPrerendered(string assembly, string typeName, string parameterDefinitions, string parameterValues) =>
        new WebAssemblyComponentMarker(ClientMarkerType, assembly, typeName, parameterDefinitions, parameterValues, null);

    internal static WebAssemblyComponentMarker Prerendered(string assembly, string typeName, string parameterDefinitions, string parameterValues) =>
        new WebAssemblyComponentMarker(ClientMarkerType, assembly, typeName, parameterDefinitions, parameterValues, Guid.NewGuid().ToString("N"));

    public WebAssemblyEndComponentMarker GetEndRecord()
    {
        if (PrerenderId == null)
        {
            throw new InvalidOperationException("Can't get an end record for non-prerendered components.");
        }

        return new WebAssemblyEndComponentMarker { PrerenderId = PrerenderId };
    }
}

internal struct WebAssemblyEndComponentMarker
{
    public string PrerenderId { get; set; }
}
