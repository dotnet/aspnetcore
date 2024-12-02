// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.InternalTesting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
public class NamedPipesSupportedAttribute : Attribute, ITestCondition
{
    public bool IsMet => OperatingSystem.IsWindows();
    public string SkipReason => "Named pipes requires Windows";
}
