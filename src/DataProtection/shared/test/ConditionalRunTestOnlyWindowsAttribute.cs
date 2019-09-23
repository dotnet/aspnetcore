// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.DataProtection.Test.Shared
{
    public class ConditionalRunTestOnlyOnWindowsAttribute : Attribute, ITestCondition
    {
        public bool IsMet => OSVersionUtil.IsWindows();

        public string SkipReason { get; } = "Test requires Windows 7 / Windows Server 2008 R2 or higher.";
    }
}
