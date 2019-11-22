// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class SkipInVSTSAttribute : Attribute, ITestCondition
    {
        public static bool RunningInVSTS = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SYSTEM_TASKDEFINITIONSURI"));
        public bool IsMet => !RunningInVSTS;

        public string SkipReason => "Running in VSTS";
    }
}
