// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.AspNetCore.WebSockets.Internal.ConformanceTest.Autobahn;

namespace Microsoft.AspNetCore.WebSockets.Internal.ConformanceTest
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfWsTestNotPresentAttribute : Attribute, ITestCondition
    {
        public bool IsMet => Wstest.Default != null;
        public string SkipReason => "Autobahn Test Suite is not installed on the host machine.";
    }
}