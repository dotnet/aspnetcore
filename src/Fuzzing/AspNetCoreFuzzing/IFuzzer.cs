// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AspNetCoreFuzzing;

/// <summary>
/// Interface for defining a fuzzing target.
/// Implementations are discovered via reflection and automatically
/// become available for local runs and OneFuzz deployments.
/// </summary>
internal interface IFuzzer
{
    /// <summary>Friendly name to identify this fuzz target. Defaults to the class name.</summary>
    string Name => GetType().Name;

    /// <summary>
    /// List of assemblies that should be instrumented.
    /// These are the assemblies where the code under test lives.
    /// </summary>
    /// <example>
    /// <code>
    /// public string[] TargetAssemblies => ["Microsoft.Net.Http.Headers"];
    /// </code>
    /// </example>
    string[] TargetAssemblies { get; }

    /// <summary>Optional name of the dictionary file to use to better guide the fuzzer.</summary>
    string? Dictionary => null;

    /// <summary>Optional name of the directory to use as an initial corpus for the fuzzer.</summary>
    string? Corpus => null;

    /// <summary>
    /// Entry point for the fuzzer. Should exercise code paths in <see cref="TargetAssemblies"/>.
    /// </summary>
    /// <param name="bytes">The fuzzer-generated input bytes.</param>
    void FuzzTarget(ReadOnlySpan<byte> bytes);
}
