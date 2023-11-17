// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.InternalTesting;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class FrameworkSkipConditionAttribute : Attribute, ITestCondition
{
    private readonly RuntimeFrameworks _excludedFrameworks;

    public FrameworkSkipConditionAttribute(RuntimeFrameworks excludedFrameworks)
    {
        _excludedFrameworks = excludedFrameworks;
    }

    public bool IsMet
    {
        get
        {
            return CanRunOnThisFramework(_excludedFrameworks);
        }
    }

    public string SkipReason { get; set; } = "Test cannot run on this runtime framework.";

    private static bool CanRunOnThisFramework(RuntimeFrameworks excludedFrameworks)
    {
        if (excludedFrameworks == RuntimeFrameworks.None)
        {
            return true;
        }

        if (excludedFrameworks.HasFlag(RuntimeFrameworks.Mono) &&
            TestPlatformHelper.IsMono)
        {
            return false;
        }

#if NETFRAMEWORK
        if (excludedFrameworks.HasFlag(RuntimeFrameworks.CLR))
        {
            return false;
        }
#elif NETSTANDARD2_0 || NET6_0_OR_GREATER
        if (excludedFrameworks.HasFlag(RuntimeFrameworks.CoreCLR))
        {
            return false;
        }
#else
#error Target frameworks need to be updated.
#endif
        return true;
    }
}
