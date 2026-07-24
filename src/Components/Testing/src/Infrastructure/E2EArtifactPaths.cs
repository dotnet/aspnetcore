// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Public helpers that resolve the well-known per-test artifact directories used by the
/// E2E testing infrastructure. These are intended for source-generated adapter code in
/// the consumer test assembly, which needs the same directory layout the library uses
/// internally (see the internal <c>E2EArtifacts</c> resolver) but cannot access it.
/// </summary>
public static class E2EArtifactPaths
{
    /// <summary>
    /// The directory that per-test artifacts (screenshots, console logs, Playwright
    /// traces and videos) for <paramref name="testName"/> are written to.
    /// </summary>
    /// <param name="testName">The current test name (for example MSTest's <c>TestContext.TestName</c>).</param>
    /// <returns>An absolute directory path under the resolved artifacts root (not created on disk).</returns>
    public static string ForTest(string testName)
        => E2EArtifacts.GetPath(PlaywrightExtensions.SanitizeFileName(testName ?? "unknown"));

    /// <summary>
    /// The directory that captured server stdout/stderr for <paramref name="testName"/>
    /// is written to.
    /// </summary>
    /// <param name="testName">The current test name (for example MSTest's <c>TestContext.TestName</c>).</param>
    /// <returns>An absolute directory path under the resolved artifacts root (not created on disk).</returns>
    public static string ServerOutput(string testName)
        => E2EArtifacts.GetPath("server-output", PlaywrightExtensions.SanitizeFileName(testName ?? "unknown"));
}
