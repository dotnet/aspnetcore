// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !CORVUS_PROGRAM_IMAGE
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Corvus.Text.Json;
using Corvus.Text.Json.RuntimeEvaluator;
using Json.Schema;

namespace GeneratedSchemaInspection;

[MemoryDiagnoser]
public class GenerationBenchmarks
{
    private static readonly byte[] s_valid =
        """{"isActive":false,"email":"person@example.com","address":{"country":"GB"}}"""u8.ToArray();
    private static readonly byte[] s_invalid =
        """{"isActive":true,"email":"person@example.com","displayName":"x","address":{"country":"GB"}}"""u8.ToArray();

    // Manual evidence prototype. This is not claimed to be upstream output.
    private static readonly byte[] s_bundle =
        """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "$id": "urn:jsonschema:GeneratedSchemaInspection.FlagshipModel",
          "type": "object",
          "properties": {
            "isActive": { "type": "boolean" },
            "email": { "type": "string" },
            "address": {
              "anyOf": [
                { "$ref": "#/$defs/FlagshipAddress" },
                { "type": "null" }
              ]
            }
          },
          "required": [ "isActive", "email" ],
          "if": {
            "properties": { "isActive": { "const": true } },
            "required": [ "isActive" ]
          },
          "then": {
            "properties": {
              "displayName": {
                "type": [ "string", "null" ],
                "minLength": 3
              }
            }
          },
          "unevaluatedProperties": false,
          "additionalProperties": false,
          "$defs": {
            "FlagshipAddress": {
              "type": "object",
              "properties": { "country": { "type": "string" } },
              "required": [ "country" ]
            }
          }
        }
        """u8.ToArray();

    private JsonSchema _nativeRoot = null!;
    private byte[] _supportedBundleBytes = null!;
    private JsonSchema _parsedBundle = null!;
    private EvaluationOptions _nativeOptions = null!;
    private EvaluationOptions _bundleOptions = null!;
    private Corvus.Text.Json.Validator.JsonSchema _corvus;

    [GlobalSetup]
    public void Setup()
    {
        _nativeRoot = global::JsonSchemaNetGenerationInspection.GeneratedJsonSchemas.FlagshipModel;
        _nativeOptions = new EvaluationOptions
        {
            OutputFormat = OutputFormat.List,
        };
        _supportedBundleBytes = JsonSerializer.SerializeToUtf8Bytes(CreateSupportedBundleCore());
        _parsedBundle = JsonSchema.FromText(
            System.Text.Encoding.UTF8.GetString(_supportedBundleBytes),
            new BuildOptions
            {
                Dialect = Dialect.Draft202012,
                SchemaRegistry = new SchemaRegistry(),
            });
        _bundleOptions = new EvaluationOptions { OutputFormat = OutputFormat.List };
        _corvus = Corvus.Text.Json.Validator.JsonSchema.FromText(
            System.Text.Encoding.UTF8.GetString(_supportedBundleBytes),
            "urn:jsonschema:GeneratedSchemaInspection.FlagshipBundle",
            new Corvus.Text.Json.Validator.JsonSchema.Options(
                allowFileSystemAndHttpResolution: false,
                defaultDialect: JsonSchemaDialect.Draft202012,
                alwaysAssertFormat: false));

        using var valid = JsonDocument.Parse(s_valid);
        using var invalid = JsonDocument.Parse(s_invalid);
        var nativeValid = _nativeRoot.Evaluate(valid.RootElement, _nativeOptions).IsValid;
        var nativeInvalid = _nativeRoot.Evaluate(invalid.RootElement, _nativeOptions).IsValid;
        var bundleValid = _parsedBundle.Evaluate(valid.RootElement, _bundleOptions).IsValid;
        var bundleInvalid = _parsedBundle.Evaluate(invalid.RootElement, _bundleOptions).IsValid;
        var corvusValid = _corvus.Validate(s_valid);
        var corvusInvalid = _corvus.Validate(s_invalid);
        if (!nativeValid || nativeInvalid || !bundleValid || bundleInvalid || !corvusValid || corvusInvalid)
        {
            throw new InvalidOperationException(
                $"The engines did not agree: native={nativeValid}/{nativeInvalid}, " +
                $"bundle={bundleValid}/{bundleInvalid}, Corvus={corvusValid}/{corvusInvalid}.");
        }
    }

    [Benchmark(Baseline = true)]
    public JsonSchema WarmStaticFieldAccess()
        => global::JsonSchemaNetGenerationInspection.GeneratedJsonSchemas.FlagshipModel;

    [Benchmark]
    public JsonSchema RebuildNativeResourceGraph()
    {
        var options = new BuildOptions
        {
            SchemaRegistry = new SchemaRegistry(),
        };
        _ = BuildAddress(options);
        return BuildRoot(options);
    }

    [Benchmark]
    public int SerializeRootOnly()
        => JsonSerializer.SerializeToUtf8Bytes(_nativeRoot).Length;

    [Benchmark]
    public int CopyManualCanonicalBundle() => s_bundle.AsSpan().ToArray().Length;

    [Benchmark]
    public JsonSchema CreateSupportedBundle() => CreateSupportedBundleCore();

    [Benchmark]
    public JsonSchema ParseSupportedBundle()
        => JsonSchema.FromText(
            System.Text.Encoding.UTF8.GetString(_supportedBundleBytes),
            new BuildOptions
            {
                Dialect = Dialect.Draft202012,
                SchemaRegistry = new SchemaRegistry(),
            });

    [Benchmark]
    public Corvus.Text.Json.Validator.JsonSchema CompileCorvusFromBundle()
        => Corvus.Text.Json.Validator.JsonSchema.FromText(
            System.Text.Encoding.UTF8.GetString(_supportedBundleBytes),
            "urn:jsonschema:GeneratedSchemaInspection.FlagshipBundle",
            new Corvus.Text.Json.Validator.JsonSchema.Options(
                allowFileSystemAndHttpResolution: false,
                defaultDialect: JsonSchemaDialect.Draft202012,
                alwaysAssertFormat: false));

    [Benchmark]
    public bool NativeValid()
    {
        using var document = JsonDocument.Parse(s_valid);
        return _nativeRoot.Evaluate(document.RootElement, _nativeOptions).IsValid;
    }

    [Benchmark]
    public bool NativeInvalid()
    {
        using var document = JsonDocument.Parse(s_invalid);
        return _nativeRoot.Evaluate(document.RootElement, _nativeOptions).IsValid;
    }

    [Benchmark]
    public bool ParsedBundleValid()
    {
        using var document = JsonDocument.Parse(s_valid);
        return _parsedBundle.Evaluate(document.RootElement, _bundleOptions).IsValid;
    }

    [Benchmark]
    public bool ParsedBundleInvalid()
    {
        using var document = JsonDocument.Parse(s_invalid);
        return _parsedBundle.Evaluate(document.RootElement, _bundleOptions).IsValid;
    }

    [Benchmark]
    public bool CorvusBundleValid() => _corvus.Validate(s_valid);

    [Benchmark]
    public bool CorvusBundleInvalid() => _corvus.Validate(s_invalid);

    private static JsonSchema CreateSupportedBundleCore()
    {
        var registry = new SchemaRegistry();
        registry.Register(global::JsonSchemaNetGenerationInspection.GeneratedJsonSchemas.FlagshipModel);
        registry.Register(global::JsonSchemaNetGenerationInspection.GeneratedJsonSchemas.FlagshipAddress);
        return registry.CreateBundle(
            new Uri("urn:jsonschema:GeneratedSchemaInspection.FlagshipModel"),
            new Uri("urn:jsonschema:GeneratedSchemaInspection.FlagshipBundle"),
            new BuildOptions
            {
                Dialect = Dialect.Draft202012,
                SchemaRegistry = registry,
            })!;
    }

    internal static byte[] CreateSupportedBundleBytes()
        => JsonSerializer.SerializeToUtf8Bytes(CreateSupportedBundleCore());

    private static JsonSchema BuildRoot(BuildOptions? options = null)
        => new JsonSchemaBuilder()
            .Schema("https://json-schema.org/draft/2020-12/schema")
            .Id("urn:jsonschema:GeneratedSchemaInspection.FlagshipModel")
            .Type(SchemaValueType.Object)
            .Properties(
                ("isActive", new JsonSchemaBuilder().Type(SchemaValueType.Boolean)),
                ("email", new JsonSchemaBuilder().Type(SchemaValueType.String)),
                ("address", new JsonSchemaBuilder().AnyOf(
                    new JsonSchemaBuilder().Ref("urn:jsonschema:GeneratedSchemaInspection.FlagshipAddress"),
                    new JsonSchemaBuilder().Type(SchemaValueType.Null))))
            .Required("isActive", "email")
            .If(new JsonSchemaBuilder()
                .Properties(("isActive", new JsonSchemaBuilder().Const(true)))
                .Required("isActive"))
            .Then(new JsonSchemaBuilder()
                .Properties(("displayName", new JsonSchemaBuilder()
                    .Type(SchemaValueType.String, SchemaValueType.Null)
                    .MinLength(3))))
            .UnevaluatedProperties(false)
            .AdditionalProperties(false)
            .Build(options);

    private static JsonSchema BuildAddress(BuildOptions? options = null)
        => new JsonSchemaBuilder()
            .Schema("https://json-schema.org/draft/2020-12/schema")
            .Id("urn:jsonschema:GeneratedSchemaInspection.FlagshipAddress")
            .Type(SchemaValueType.Object)
            .Properties(("country", new JsonSchemaBuilder().Type(SchemaValueType.String)))
            .Required("country")
            .Build(options);
}
#endif
