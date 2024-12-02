// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.InternalTesting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class OSSkipConditionAttribute : Attribute, ITestCondition
{
    private readonly OperatingSystems _excludedOperatingSystem;
    private readonly OperatingSystems _osPlatform;

    public OSSkipConditionAttribute(OperatingSystems operatingSystem) :
        this(operatingSystem, GetCurrentOS())
    {
    }

    [Obsolete("Use the Minimum/MaximumOSVersionAttribute for version checks.", error: true)]
    public OSSkipConditionAttribute(OperatingSystems operatingSystem, params string[] versions) :
        this(operatingSystem, GetCurrentOS())
    {
    }

    // to enable unit testing
    internal OSSkipConditionAttribute(OperatingSystems operatingSystem, OperatingSystems osPlatform)
    {
        _excludedOperatingSystem = operatingSystem;
        _osPlatform = osPlatform;
    }

    public bool IsMet
    {
        get
        {
            var skip = (_excludedOperatingSystem & _osPlatform) == _osPlatform;
            // Since a test would be excuted only if 'IsMet' is true, return false if we want to skip
            return !skip;
        }
    }

    public string SkipReason { get; set; } = "Test cannot run on this operating system.";

    private static OperatingSystems GetCurrentOS()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OperatingSystems.Windows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OperatingSystems.Linux;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OperatingSystems.MacOSX;
        }
        throw new PlatformNotSupportedException();
    }
}
