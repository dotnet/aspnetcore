// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

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
