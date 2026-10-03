// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Corvus.Text.Json.RuntimeEvaluator;
using GeneratedSchemaInspection;
using Json.Schema;
using Microsoft.AspNetCore.OpenApi;

namespace AnnotatedSchemaDemo;

[MemoryDiagnoser]
public class DemoBenchmarks
{
    private byte[] _bundle = null!;
    private byte[] _compatibilitySchema = null!;
    private byte[] _valid = null!;
    private byte[] _invalid = null!;
    private JsonSchema _parsed = null!;
    private EvaluationOptions _options = null!;
    private JsonSchemaEvaluator _compiled = null!;
    private JsonSchemaEvaluator _image = null!;
    private int _bundleIndex;

    [GlobalSetup]
    public void Setup()
    {
        _bundle = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "flagship.bundle.json"));
        _bundleIndex = Environment.TickCount & 1;
        _compatibilitySchema = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "flagship.schema.json"));
        _valid = DemoCorpus.Valid[1];
        _invalid = DemoCorpus.Invalid[3];
        _parsed = JsonSchema.FromText(
            Encoding.UTF8.GetString(_compatibilitySchema),
            new BuildOptions { Dialect = Dialect.Draft202012, SchemaRegistry = new SchemaRegistry() });
        _options = new()
        {
            OutputFormat = OutputFormat.Flag,
            RequireFormatValidation = false,
        };
        _compiled = JsonSchemaEvaluator.Compile(_compatibilitySchema, DemoCorvusOptions.Create());
        _image = DemoEngineEquivalence.LoadCorvusProgramImage();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _compiled.Dispose();
        _image.Dispose();
    }

    [Benchmark]
    public int HistoricalExporterBundleConstruction()
        => HistoricalExporter.CreateSupportedBundleBytes().Length;

    [Benchmark]
    public byte SamePassArtifactByteAccess()
    {
        var bundle = GeneratedJsonSchemas.CanonicalArtifacts.FlagshipModel.BundleUtf8;
        return bundle[Volatile.Read(ref _bundleIndex)];
    }

    [Benchmark(Baseline = true)]
    public int EngineOnlyCompileCorvus()
    {
        using var evaluator = JsonSchemaEvaluator.Compile(_compatibilitySchema, DemoCorvusOptions.Create());
        return evaluator.NodeCount;
    }

    [Benchmark]
    public int EngineOnlyLoadCorvusImage()
    {
        using var evaluator = JsonSchemaEvaluator.FromProgramImage(
            FlagshipCorvusProgramImage.Bytes,
            DemoCorvusOptions.Create());
        return evaluator.NodeCount;
    }

    [Benchmark]
    public bool EngineOnlyJsonSchemaNetNativeValid()
    {
        using var document = JsonDocument.Parse(_valid);
        return GeneratedJsonSchemas.FlagshipModel.Evaluate(document.RootElement, _options).IsValid;
    }

    [Benchmark]
    public bool EngineOnlyJsonSchemaNetNativeInvalid()
    {
        using var document = JsonDocument.Parse(_invalid);
        return GeneratedJsonSchemas.FlagshipModel.Evaluate(document.RootElement, _options).IsValid;
    }

    [Benchmark]
    public bool EngineOnlyJsonSchemaNetParsedBundleValid()
    {
        using var document = JsonDocument.Parse(_valid);
        return _parsed.Evaluate(document.RootElement, _options).IsValid;
    }

    [Benchmark]
    public bool EngineOnlyJsonSchemaNetParsedBundleInvalid()
    {
        using var document = JsonDocument.Parse(_invalid);
        return _parsed.Evaluate(document.RootElement, _options).IsValid;
    }

    [Benchmark]
    public bool EngineOnlyCorvusImageValid() => _image.Evaluate(_valid);

    [Benchmark]
    public bool EngineOnlyCorvusImageInvalid() => _image.Evaluate(_invalid);

    [Benchmark]
    public bool AspNetJsonSchemaNetBindingValid()
    {
        var validation = JsonSchemaNetBinding.ValidateAsync(
            _valid,
            OpenApiSchemaEvidencePurpose.Input);
        if (!validation.IsCompletedSuccessfully)
        {
            throw new InvalidOperationException("The generated binding did not complete synchronously.");
        }

        return validation.Result.IsValid;
    }

    [Benchmark]
    public bool AspNetCorvusBindingValid()
    {
        var validation = CorvusBinding.ValidateAsync(
            _valid,
            OpenApiSchemaEvidencePurpose.Input);
        if (!validation.IsCompletedSuccessfully)
        {
            throw new InvalidOperationException("The generated binding did not complete synchronously.");
        }

        return validation.Result.IsValid;
    }
}
