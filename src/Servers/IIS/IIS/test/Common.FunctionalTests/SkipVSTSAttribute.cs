// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class SkipInVSTSAttribute : Attribute, ITestCondition
    {
        public bool IsMet => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SYSTEM_TASKDEFINITIONSURI"));

        public string SkipReason => "Running in VSTS";
    }
}
