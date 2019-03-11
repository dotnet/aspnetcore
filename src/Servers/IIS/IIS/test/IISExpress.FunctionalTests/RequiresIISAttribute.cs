// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequiresIISAttribute : Attribute, ITestCondition
    {
        public bool IsMet { get ; } = IISExpressAncmSchema.SupportsInProcessHosting;

        public string SkipReason { get; } = IISExpressAncmSchema.SkipReason;

        public RequiresIISAttribute()
        {
            // https://github.com/aspnet/AspNetCore/issues/8329
            if (Environment.OSVersion.Version.Major == 6 &&
                Environment.OSVersion.Version.Minor == 1)
            {
                IsMet = false;
                SkipReason = "Skipped on Windows 7";
            }
        }

        public RequiresIISAttribute(IISCapability capabilities) : this()
        {
            // IISCapabilities aren't pertinent to IISExpress
        }
    }
}
