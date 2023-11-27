// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipInVSTSAttribute : Attribute, ITestCondition
{
    public static bool RunningInVSTS = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SYSTEM_TASKDEFINITIONSURI"));
    public bool IsMet => !RunningInVSTS;

    public string SkipReason => "Running in VSTS";
}
