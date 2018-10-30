// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing.xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Microsoft.AspNetCore.Testing.xunit." + nameof(ConditionalFactDiscoverer), "Microsoft.AspNetCore.Testing")]
    public class ConditionalFactAttribute : FactAttribute
    {
    }
}