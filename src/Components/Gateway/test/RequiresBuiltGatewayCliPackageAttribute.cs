// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Components.Gateway;

/// <summary>
/// Skips a <see cref="ConditionalFactAttribute"/> test when the required locally-built .nupkg files
/// have not been produced (for example on a fresh clone or a CI leg that does not pack), so these
/// packaging tests only run where the packages are available.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequiresBuiltGatewayCliPackageAttribute : Attribute, ITestCondition
{
    public bool IsMet => GatewayCliTestData.TryGetPackagePath(GatewayCliTestData.PackageId) is not null;

    public string SkipReason =>
        $"The '{GatewayCliTestData.PackageId}' package was not built. " +
        "Pack the project (e.g. './eng/build.sh -pack') before running these tests.";
}
