// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit.Abstractions;
using Xunit.Sdk;

// Do not change this namespace without changing the usage in QuarantinedTestAttribute
namespace Microsoft.AspNetCore.InternalTesting;

public class QuarantinedTestTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        if (traitAttribute is ReflectionAttributeInfo { Attribute: QuarantinedTestAttribute attribute })
        {
            if (IsQuarantined(attribute, GetCurrentOperatingSystem()))
            {
                yield return new KeyValuePair<string, string>("Quarantined", "true");
            }
        }
        else
        {
            throw new InvalidOperationException("The 'QuarantinedTest' attribute is only supported via reflection.");
        }
    }

    internal static bool IsQuarantined(QuarantinedTestAttribute attribute, OperatingSystems currentOperatingSystem)
    {
        return attribute.OperatingSystems is null
            || (attribute.OperatingSystems.Value & currentOperatingSystem) == currentOperatingSystem;
    }

    internal static bool IsQuarantined(QuarantinedTestAttribute attribute)
    {
        return IsQuarantined(attribute, GetCurrentOperatingSystem());
    }

    private static OperatingSystems GetCurrentOperatingSystem()
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
