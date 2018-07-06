// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class SkipIfHostableWebCoreNotAvailibleAttribute : Attribute, ITestCondition
    {
        public bool IsMet { get; } = File.Exists(TestServer.HostableWebCoreLocation);

        public string SkipReason { get; } = $"Hostable Web Core not availible, {TestServer.HostableWebCoreLocation} not found.";
    }
}
