// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Json.Schema;

if (args is ["--root-only-probe"])
{
    const string rootOnlySchema = """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "$id": "urn:jsonschema:GeneratedSchemaInspection.FlagshipModel",
          "type": "object",
          "properties": {
            "address": {
              "anyOf": [
                { "$ref": "urn:jsonschema:GeneratedSchemaInspection.FlagshipAddress" },
                { "type": "null" }
              ]
            }
          }
        }
        """;
    const string instance = """{"address":{"country":"US"}}""";

    try
    {
        var schema = JsonSchema.FromText(
            rootOnlySchema,
            new BuildOptions
            {
                Dialect = Dialect.Draft202012,
                SchemaRegistry = new SchemaRegistry(),
            });
        using var document = JsonDocument.Parse(instance);
        var result = schema.Evaluate(document.RootElement, new EvaluationOptions());
        Console.WriteLine($"unexpected-result:{result.IsValid}");
        return 2;
    }
    catch (RefResolutionException exception)
    {
        Console.WriteLine($"{exception.GetType().Name}:{exception.Message}");
        return 0;
    }
}

GC.KeepAlive(typeof(JsonSchema));
return 0;
