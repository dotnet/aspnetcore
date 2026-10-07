// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if CORVUS_PROGRAM_IMAGE
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Corvus.Text.Json.RuntimeEvaluator;
using Json.Schema;

namespace GeneratedSchemaInspection;

[MemoryDiagnoser]
public class ProgramImageBenchmarks
{
    private static readonly byte[] s_valid =
        """{"isActive":false,"email":"person@example.com","address":{"country":"GB"}}"""u8.ToArray();
    private static readonly byte[] s_invalid =
        """{"isActive":true,"email":"person@example.com","displayName":"x","address":{"country":"GB"}}"""u8.ToArray();
    private byte[] _schema = null!;
    private JsonSchemaEvaluator _compiled = null!;
    private JsonSchemaEvaluator _image = null!;
    private Json.Schema.JsonSchema _nativeRoot = null!;
    private EvaluationOptions _nativeOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        _schema = LoadCanonicalSchema();
        _compiled = JsonSchemaEvaluator.Compile(_schema, CreateOptions());
        _image = JsonSchemaEvaluator.FromProgramImage(FlagshipCorvusProgramImage.Bytes, CreateOptions());
        _nativeRoot = global::JsonSchemaNetGenerationInspection.GeneratedJsonSchemas.FlagshipModel;
        _nativeOptions = new EvaluationOptions { OutputFormat = OutputFormat.List };
        VerifyAgreement(_compiled, _image, _nativeRoot, _nativeOptions);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _compiled.Dispose();
        _image.Dispose();
    }

    [Benchmark(Baseline = true)]
    public int CompileBundle()
    {
        using var evaluator = JsonSchemaEvaluator.Compile(_schema, CreateOptions());
        return evaluator.NodeCount;
    }

    [Benchmark]
    public int LoadProgramImage()
    {
        using var evaluator = JsonSchemaEvaluator.FromProgramImage(FlagshipCorvusProgramImage.Bytes, CreateOptions());
        return evaluator.NodeCount;
    }

    [Benchmark]
    public bool CompiledValid() => _compiled.Evaluate(s_valid);

    [Benchmark]
    public bool CompiledInvalid() => _compiled.Evaluate(s_invalid);

    [Benchmark]
    public bool ImageValid() => _image.Evaluate(s_valid);

    [Benchmark]
    public bool ImageInvalid() => _image.Evaluate(s_invalid);

    [Benchmark]
    public bool JsonSchemaNetNativeValid()
    {
        using var document = System.Text.Json.JsonDocument.Parse(s_valid);
        return _nativeRoot.Evaluate(document.RootElement, _nativeOptions).IsValid;
    }

    [Benchmark]
    public bool JsonSchemaNetNativeInvalid()
    {
        using var document = System.Text.Json.JsonDocument.Parse(s_invalid);
        return _nativeRoot.Evaluate(document.RootElement, _nativeOptions).IsValid;
    }

    internal static void Verify()
    {
        var schema = LoadCanonicalSchema();
        using var compiled = JsonSchemaEvaluator.Compile(schema, CreateOptions());
        compiled.RegisterEntryPoints(["urn:jsonschema:GeneratedSchemaInspection.FlagshipModel"]);
        var roundTripImage = compiled.ToProgramImage();
        if (!roundTripImage.AsSpan().SequenceEqual(FlagshipCorvusProgramImage.Bytes.Span))
        {
            throw new InvalidOperationException("The generated image does not round-trip byte-for-byte.");
        }

        if (JsonSchemaEvaluator.GetImagePatterns(roundTripImage).Count != FlagshipCorvusProgramImage.PatternCount)
        {
            throw new InvalidOperationException("The generated pattern metadata does not match the image.");
        }

        using var image = CorvusImageBinding.Load(
            FlagshipCorvusProgramImage.SchemaGraphIdentity,
            FlagshipCorvusProgramImage.ConfigurationIdentity);
        var native = global::JsonSchemaNetGenerationInspection.GeneratedJsonSchemas.FlagshipModel;
        VerifyAgreement(compiled, image, native, new EvaluationOptions { OutputFormat = OutputFormat.List });

        var corrupt = (byte[])roundTripImage.Clone();
        corrupt[4]++;
        try
        {
            using var _ = JsonSchemaEvaluator.FromProgramImage(corrupt, CreateOptions());
            throw new InvalidOperationException("A mismatched image version was accepted.");
        }
        catch (JsonSchemaCompilationException)
        {
        }

        try
        {
            using var _ = CorvusImageBinding.Load(
                FlagshipCorvusProgramImage.SchemaGraphIdentity,
                "incompatible-configuration");
            throw new InvalidOperationException("A mismatched validator configuration was accepted.");
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("configuration", StringComparison.Ordinal))
        {
        }

        var sharedEvaluators = new JsonSchemaEvaluator[8];
        Parallel.For(0, sharedEvaluators.Length, index => sharedEvaluators[index] = CorvusImageBinding.Shared);
        if (Array.Exists(sharedEvaluators, evaluator => !ReferenceEquals(evaluator, sharedEvaluators[0])))
        {
            throw new InvalidOperationException("The lazy image binding constructed multiple evaluator instances.");
        }
    }

    internal static void RunWorker(string operation)
    {
        var schema = LoadCanonicalSchema();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        using var evaluator = operation switch
        {
            "compile" => JsonSchemaEvaluator.Compile(schema, CreateOptions()),
            "image" => JsonSchemaEvaluator.FromProgramImage(FlagshipCorvusProgramImage.Bytes, CreateOptions()),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
        var elapsed = Stopwatch.GetElapsedTime(start);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Console.WriteLine($"{elapsed.TotalNanoseconds:F0},{allocated}");
    }

    private static JsonSchemaEvaluatorOptions CreateOptions()
        => new()
        {
            DefaultDialect = JsonSchemaDialect.Draft202012,
            AssertFormat = false,
            AssertContent = true,
            CompileRegularExpressions = false,
            BaseUri = "urn:jsonschema:GeneratedSchemaInspection.FlagshipBundle",
        };

    private static byte[] LoadCanonicalSchema()
        => System.Text.Encoding.UTF8.GetBytes(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "flagship.bundle.json")).TrimEnd('\r', '\n'));

    private static void VerifyAgreement(
        JsonSchemaEvaluator compiled,
        JsonSchemaEvaluator image,
        Json.Schema.JsonSchema native,
        EvaluationOptions nativeOptions)
    {
        using var valid = System.Text.Json.JsonDocument.Parse(s_valid);
        using var invalid = System.Text.Json.JsonDocument.Parse(s_invalid);
        var results = new[]
        {
            compiled.Evaluate(s_valid),
            !compiled.Evaluate(s_invalid),
            image.Evaluate(s_valid),
            !image.Evaluate(s_invalid),
            native.Evaluate(valid.RootElement, nativeOptions).IsValid,
            !native.Evaluate(invalid.RootElement, nativeOptions).IsValid,
        };
        if (Array.IndexOf(results, false) >= 0)
        {
            throw new InvalidOperationException($"Evaluator disagreement: {string.Join(",", results)}");
        }
    }

    private static class CorvusImageBinding
    {
        private static readonly Lazy<JsonSchemaEvaluator> s_evaluator = new(
            static () => JsonSchemaEvaluator.FromProgramImage(FlagshipCorvusProgramImage.Bytes, CreateOptions()),
            LazyThreadSafetyMode.ExecutionAndPublication);

        internal static JsonSchemaEvaluator Shared => s_evaluator.Value;

        internal static JsonSchemaEvaluator Load(string schemaIdentity, string configurationIdentity)
        {
            if (!string.Equals(schemaIdentity, FlagshipCorvusProgramImage.SchemaGraphIdentity, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The schema graph identity is incompatible with this image.");
            }

            if (!string.Equals(
                configurationIdentity,
                FlagshipCorvusProgramImage.ConfigurationIdentity,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The validator configuration is incompatible with this image.");
            }

            return JsonSchemaEvaluator.FromProgramImage(FlagshipCorvusProgramImage.Bytes, CreateOptions());
        }
    }
}
#endif
