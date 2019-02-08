// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Testing.xunit
{
    /// <summary>
    /// Skip test if a given environment variable is not enabled. To enable the test, set environment variable
    /// to "true" for the test process.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipOnHelixAttribute : Attribute, ITestCondition
    {
        public bool IsMet
        {
            get
            {
                return !string.Equals(Environment.GetEnvironmentVariable("helix"), "true", StringComparison.OrdinalIgnoreCase);
            }
        }

        public string SkipReason
        {
            get
            {
                return $"This test is skipped on helix";
            }
        }
    }
}
