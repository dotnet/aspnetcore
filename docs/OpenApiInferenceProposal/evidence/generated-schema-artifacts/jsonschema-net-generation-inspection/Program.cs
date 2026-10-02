// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using BenchmarkDotNet.Running;
#if !CORVUS_PROGRAM_IMAGE
using Corvus.Text.Json.RuntimeEvaluator;
using GeneratedSchemaInspection;
#endif

#if !CORVUS_PROGRAM_IMAGE
if (args is ["--cold-worker"])
{
    var start = Stopwatch.GetTimestamp();
    _ = JsonSchemaNetGenerationInspection.GeneratedJsonSchemas.FlagshipModel;
    Console.WriteLine(Stopwatch.GetElapsedTime(start).TotalNanoseconds);
    return;
}

if (args is ["--corvus-compile-worker"])
{
    var bytes = GenerationBenchmarks.CreateSupportedBundleBytes();
    var text = System.Text.Encoding.UTF8.GetString(bytes);
    var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
    var start = Stopwatch.GetTimestamp();
    var schema = Corvus.Text.Json.Validator.JsonSchema.FromText(
        text,
        "urn:jsonschema:GeneratedSchemaInspection.FlagshipBundle",
        new Corvus.Text.Json.Validator.JsonSchema.Options(
            allowFileSystemAndHttpResolution: false,
            defaultDialect: JsonSchemaDialect.Draft202012,
            alwaysAssertFormat: false));
    var elapsed = Stopwatch.GetElapsedTime(start);
    var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
    GC.KeepAlive(schema);
    Console.WriteLine($"{elapsed.TotalNanoseconds:F0},{allocated}");
    return;
}

if (args is ["--verify"])
{
    new GenerationBenchmarks().Setup();
    Console.WriteLine("verified");
    return;
}

if (args is ["--describe"])
{
    var rootBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
        JsonSchemaNetGenerationInspection.GeneratedJsonSchemas.FlagshipModel);
    var bundleBytes = GenerationBenchmarks.CreateSupportedBundleBytes();
    Console.WriteLine($"root={rootBytes.Length}:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(rootBytes))}");
    Console.WriteLine($"bundle={bundleBytes.Length}:{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bundleBytes))}");
    return;
}

if (args is ["--print-bundle"])
{
    Console.WriteLine(System.Text.Encoding.UTF8.GetString(GenerationBenchmarks.CreateSupportedBundleBytes()));
    return;
}
#else
if (args is ["--verify-image"])
{
    GeneratedSchemaInspection.ProgramImageBenchmarks.Verify();
    Console.WriteLine("verified");
    return;
}

if (args is ["--image-worker", var operation])
{
    GeneratedSchemaInspection.ProgramImageBenchmarks.RunWorker(operation);
    return;
}
#endif

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
