// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Testing.xunit;

namespace Interop.FunctionalTests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SkipIfChromeUnavailableAttribute : Attribute, ITestCondition
    {
        public bool IsMet => string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_HOME")) && (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) || File.Exists(ChromeConstants.ExecutablePath));

        public string SkipReason => "This is running on Jenkins or Chrome/Chromium is not installed and this is a dev environment.";
    }
}
