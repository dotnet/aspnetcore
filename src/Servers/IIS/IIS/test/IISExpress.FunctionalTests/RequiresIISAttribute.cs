// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequiresIISAttribute : Attribute, ITestCondition
    {
        public bool IsMet => IISExpressAncmSchema.SupportsInProcessHosting;

        public string SkipReason => IISExpressAncmSchema.SkipReason;

        public RequiresIISAttribute() { }

        public RequiresIISAttribute(IISCapability capabilities)
        {
            // IISCapabilities aren't pertinent to IISExpress
        }
    }
}
