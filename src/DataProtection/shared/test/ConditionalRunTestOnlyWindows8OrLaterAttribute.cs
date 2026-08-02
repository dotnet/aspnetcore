// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.DataProtection.Test.Shared;

public class ConditionalRunTestOnlyOnWindows8OrLaterAttribute : Attribute, ITestCondition
{
    public bool IsMet => OSVersionUtil.IsWindows8OrLater();

    public string SkipReason { get; } = "Test requires Windows 8 / Windows Server 2012 or higher.";
}
