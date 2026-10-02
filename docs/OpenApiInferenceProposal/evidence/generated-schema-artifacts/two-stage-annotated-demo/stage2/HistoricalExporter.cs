// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using GeneratedSchemaInspection;
using Json.Schema;

namespace AnnotatedSchemaDemo;

internal static class HistoricalExporter
{
    internal static byte[] CreateSupportedBundleBytes()
    {
        var registry = new SchemaRegistry();
        registry.Register(GeneratedJsonSchemas.FlagshipModel);
        registry.Register(GeneratedJsonSchemas.FlagshipAddress);
        var bundle = registry.CreateBundle(
            new Uri("urn:flagship:conditional"),
            new Uri("urn:flagship:historical-bundle"),
            new BuildOptions { Dialect = Dialect.Draft202012, SchemaRegistry = registry })
            ?? throw new InvalidOperationException("The native graph could not be bundled.");
        return JsonSerializer.SerializeToUtf8Bytes(bundle);
    }
}
