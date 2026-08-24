// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Provides the test-framework integration required to retain and publish test artifacts.
/// </summary>
public interface ITestArtifactManager
{
    /// <summary>
    /// Determines whether artifacts produced by the current test should be retained.
    /// </summary>
    /// <returns><see langword="true"/> to retain the artifacts; otherwise, <see langword="false"/>.</returns>
    bool ShouldSaveArtifacts();

    /// <summary>
    /// Allocates a directory for a group of artifacts produced by the current test.
    /// </summary>
    /// <param name="category">The artifact category.</param>
    /// <returns>An absolute directory path reserved for the artifact group.</returns>
    string CreateArtifactDirectory(string category);

    /// <summary>
    /// Publishes retained artifact files to the active test framework.
    /// </summary>
    /// <param name="paths">The absolute paths of the retained artifact files.</param>
    void AddArtifacts(IReadOnlyList<string> paths);
}
