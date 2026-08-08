// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// The serializer options used to persist and restore component and service state.
/// </summary>
/// <remarks>
/// The chain resolves the framework's own generated contracts first and falls back to reflection only
/// when reflection-based serialization is enabled at all, which Native AOT disables by default. That
/// keeps an existing application byte-for-byte unaffected while letting a native application persist
/// the state the framework itself owns.
/// </remarks>
internal static class PersistentStateSerializationOptions
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptionsProvider.Options);

        // Copying the options materialized whatever resolver the source instance had, so the chain is
        // reset before the ordered one is built.
        options.TypeInfoResolverChain.Clear();
        options.TypeInfoResolverChain.Add(PersistentStateJsonContext.Default);

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            options.TypeInfoResolverChain.Add(CreateReflectionResolver());
        }

        return options;
    }

    /// <summary>
    /// Contributes contracts for types the framework itself cannot name, ahead of the reflection
    /// fallback so that a generated contract always wins over a reflected one.
    /// </summary>
    /// <remarks>
    /// Called while services are being registered, which is before any state is persisted or restored,
    /// so the options are still writable. A resolver already in the chain is not added twice, because
    /// registering component metadata more than once is supported.
    /// </remarks>
    public static void AddResolver(IJsonTypeInfoResolver resolver)
    {
        var chain = Options.TypeInfoResolverChain;
        if (chain.Contains(resolver))
        {
            return;
        }

        var insertAt = JsonSerializer.IsReflectionEnabledByDefault ? chain.Count - 1 : chain.Count;
        chain.Insert(insertAt, resolver);
    }

    /// <summary>
    /// Determines whether a contract is available for <paramref name="type"/>.
    /// </summary>
    /// <remarks>
    /// State whose contract cannot be resolved is skipped rather than persisted, because throwing
    /// would abort the whole pause and leave the application without any persisted state at all.
    /// </remarks>
    public static bool CanSerialize(Type type)
    {
        try
        {
            return Options.TryGetTypeInfo(type, out _);
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "Guarded by JsonSerializer.IsReflectionEnabledByDefault.")]
    private static DefaultJsonTypeInfoResolver CreateReflectionResolver() => new();
}

/// <summary>
/// Contracts for the state the framework itself persists.
/// </summary>
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTimeOffset))]
internal sealed partial class PersistentStateJsonContext : JsonSerializerContext;
