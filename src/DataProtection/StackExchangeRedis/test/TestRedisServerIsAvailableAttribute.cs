// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing;
using System;

namespace Microsoft.AspNetCore.DataProtection
{
    internal class TestRedisServerIsAvailableAttribute : Attribute, ITestCondition
    {
        public bool IsMet => !string.IsNullOrEmpty(TestRedisServer.GetConnectionString());

        public string SkipReason => $"A test redis server must be configured to run. Set the connection string as an environment variable as {TestRedisServer.ConnectionStringKeyName.Replace(":", "__")} or in testconfig.json";
    }
}