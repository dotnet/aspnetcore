// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.InternalTesting;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Microsoft.AspNetCore.InternalTesting." + nameof(ConditionalFactDiscoverer), "Microsoft.AspNetCore.InternalTesting")]
public class ConditionalFactAttribute : FactAttribute
{
}
