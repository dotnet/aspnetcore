// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Resolves the directory used for artifacts produced by an individual test.
/// </summary>
/// <remarks>
/// The artifact root is resolved once, in priority order:
/// <list type="number">
///   <item><description>
///     The <c>E2E_ARTIFACTS_DIR</c> environment variable, when set.
///   </description></item>
///   <item><description>
///     The build-injected <c>Microsoft.AspNetCore.Components.Testing.ArtifactsPath</c>
///     assembly metadata value from the entry assembly. A rooted value is used as-is;
///     a relative value is resolved against <see cref="AppContext.BaseDirectory"/>.
///   </description></item>
///   <item><description>
///     A <c>test-artifacts</c> folder under <see cref="AppContext.BaseDirectory"/>.
///   </description></item>
/// </list>
/// The machine temporary directory is never used, so artifacts remain under the
/// configured test output.
/// </remarks>
public static class TestArtifactDirectory
{
    private const string ArtifactsPathKey = "Microsoft.AspNetCore.Components.Testing.ArtifactsPath";
    private const string EnvironmentVariableName = "E2E_ARTIFACTS_DIR";
    private const string DefaultFolderName = "test-artifacts";

    private static readonly HashSet<char> s_invalidFileNameChars =
    [
        '\\', '/', ':', '*', '?', '"', '<', '>', '|', '\0',
        .. Enumerable.Range(1, 31).Select(i => (char)i)
    ];

    private static readonly string s_root = ResolveRoot();

    /// <summary>
    /// Gets the artifact directory path for the specified test.
    /// </summary>
    /// <param name="testName">The test name used to identify the directory.</param>
    /// <returns>The artifact directory path. The directory is not created.</returns>
    public static string GetPath(string testName)
    {
        var sanitizedTestName = string.Concat(testName.Select(c => s_invalidFileNameChars.Contains(c) ? '_' : c));

        return Path.Combine(s_root, sanitizedTestName);
    }

    private static string ResolveRoot()
    {
        var overrideDirectory = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrEmpty(overrideDirectory))
        {
            return overrideDirectory;
        }

        var configuredPath = GetConfiguredPath();
        if (!string.IsNullOrEmpty(configuredPath))
        {
            return Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(AppContext.BaseDirectory, configuredPath);
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
