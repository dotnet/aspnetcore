// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Components.WebView.StaticWebAssets;

/// <summary>
/// Skips a <see cref="ConditionalFactAttribute"/> test when the required locally-built .nupkg files
/// have not been produced (for example on a fresh clone or a CI leg that does not pack), so these
/// packaging tests only run where the packages are available.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresBuiltPackagesAttribute : Attribute, ITestCondition
{
    private readonly string[] _packageIds;

    public RequiresBuiltPackagesAttribute(params string[] packageIds)
    {
        _packageIds = packageIds;
    }

    public bool IsMet => MissingPackages.Count == 0;

    public string SkipReason =>
        $"Required package(s) were not built: {string.Join(", ", MissingPackages)}. " +
        $"Pack the projects (e.g. './eng/build.cmd -pack') before running these tests.";

    private List<string> MissingPackages
        => _packageIds.Where(id => StaticWebAssetsTestData.TryGetPackagePath(id) is null).ToList();
}
