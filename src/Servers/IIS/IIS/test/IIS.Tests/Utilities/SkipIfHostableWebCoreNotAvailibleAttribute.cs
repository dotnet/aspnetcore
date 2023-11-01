// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipIfHostableWebCoreNotAvailableAttribute : Attribute, ITestCondition
{
    public bool IsMet { get; } = File.Exists(TestServer.HostableWebCoreLocation);

    public string SkipReason { get; } = $"Hostable Web Core not available, {TestServer.HostableWebCoreLocation} not found.";
}
