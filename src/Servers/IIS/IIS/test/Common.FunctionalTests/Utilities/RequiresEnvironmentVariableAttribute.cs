// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequiresEnvironmentVariableAttribute : Attribute, ITestCondition
    {
        private readonly string _name;

        public RequiresEnvironmentVariableAttribute(string name)
        {
            _name = name;
        }

        public bool IsMet => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(_name));

        public string SkipReason => $"Environment variable {_name} is required to run this test.";
    }
}
