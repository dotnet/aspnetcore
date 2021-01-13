// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequiresNewShimAttribute : Attribute, ITestCondition
    {
        public bool IsMet => DeployerSelector.HasNewShim;

        public string SkipReason => "Test verifies new behavior in the aspnetcorev2.dll that isn't supported in earlier versions.";
    }
}
