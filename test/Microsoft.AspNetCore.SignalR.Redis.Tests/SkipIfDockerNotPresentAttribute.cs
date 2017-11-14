// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.AspNetCore.SignalR.Redis.Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfDockerNotPresentAttribute : Attribute, ITestCondition
    {
        public bool IsMet => Docker.Default != null;
        public string SkipReason => "Docker is not installed on the host machine.";
    }
}
