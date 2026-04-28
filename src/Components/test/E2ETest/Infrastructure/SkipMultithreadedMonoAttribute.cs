// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using TestServer;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure;

public class SkipMultithreadedMonoAttribute : Attribute, ITestCondition
{
    public bool IsMet => !WebAssemblyTestHelper.MultithreadingIsEnabled();

    public string SkipReason { get; } = "OOP renderer is not yet supported with multi-threaded mono.";
}
