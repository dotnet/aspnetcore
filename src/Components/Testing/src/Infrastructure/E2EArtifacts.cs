// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Resolves the root directory that E2E test artifacts (screenshots, console logs,
/// server output, Playwright traces/videos) are written to.
/// </summary>
/// <remarks>
/// The root is resolved once, in priority order:
/// <list type="number">
///   <item><description>
///     The <c>E2E_ARTIFACTS_DIR</c> environment variable, when set (runtime override,
///     e.g. set by CI to redirect artifacts to an uploaded results directory).
///   </description></item>
///   <item><description>
///     The build-injected <c>[assembly: AssemblyMetadata("<see cref="ArtifactsPathKey"/>", ...)]</c>
///     value from the entry (test) assembly. A rooted value is used as-is; a relative
///     value is resolved against <see cref="AppContext.BaseDirectory"/>.
///   </description></item>
///   <item><description>
///     A <c>test-artifacts</c> folder next to the test assembly
///     (<see cref="AppContext.BaseDirectory"/>) as a last-resort fallback.
///   </description></item>
/// </list>
/// The machine temp directory is intentionally never used, so artifacts stay within
/// the build/output locations.
/// </remarks>
internal static class E2EArtifacts
{
    /// <summary>
    /// The <see cref="AssemblyMetadataAttribute.Key"/> the build injects to point the
    /// test assembly at its artifacts directory.
    /// </summary>
    internal const string ArtifactsPathKey = "Microsoft.AspNetCore.Components.Testing.ArtifactsPath";

    private const string EnvironmentVariableName = "E2E_ARTIFACTS_DIR";
    private const string DefaultFolderName = "test-artifacts";

    private static readonly string s_root = ResolveRoot();

    /// <summary>
    /// The resolved root directory for E2E test artifacts.
    /// </summary>
    public static string Root => s_root;

    /// <summary>
    /// Combines <see cref="Root"/> with the supplied path segments.
    /// </summary>
    /// <param name="segments">Path segments to append under the artifacts root.</param>
    /// <returns>The combined absolute path (not created on disk).</returns>
    public static string GetPath(params string[] segments)
    {
        if (segments is null || segments.Length == 0)
        {
            return s_root;
        }

        var parts = new string[segments.Length + 1];
        parts[0] = s_root;
        Array.Copy(segments, 0, parts, 1, segments.Length);
        return Path.Combine(parts);
    }

    private static string ResolveRoot()
    {
        var overrideDir = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrEmpty(overrideDir))
        {
            return overrideDir;
        }

        var configured = GetConfiguredPath();
        if (!string.IsNullOrEmpty(configured))
        {
            return Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppContext.BaseDirectory, configured);
        }

        return Path.Combine(AppContext.BaseDirectory, DefaultFolderName);
    }

    private static string? GetConfiguredPath()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly is null)
        {
            return null;
        }

        foreach (var metadata in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (string.Equals(metadata.Key, ArtifactsPathKey, StringComparison.Ordinal))
            {
                return metadata.Value;
            }
        }

        return null;
    }
}
