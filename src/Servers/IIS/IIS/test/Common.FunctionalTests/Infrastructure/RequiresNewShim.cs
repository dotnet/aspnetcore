// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresNewShimAttribute : Attribute, ITestCondition
{
    public bool IsMet => DeployerSelector.HasNewShim;

    public string SkipReason => "Test verifies new behavior in the aspnetcorev2.dll that isn't supported in earlier versions.";
}
