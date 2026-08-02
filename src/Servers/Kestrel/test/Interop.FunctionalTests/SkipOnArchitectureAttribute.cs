// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.InternalTesting;

namespace Interop.FunctionalTests;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class SkipOnArchitectureAttribute : Attribute, ITestCondition
{
    private readonly Architecture[] _excludedArchitectures;

    public SkipOnArchitectureAttribute(params Architecture[] excludedArchitectures)
    {
        _excludedArchitectures = excludedArchitectures;
    }

    public bool IsMet => (Array.IndexOf(_excludedArchitectures, RuntimeInformation.OSArchitecture) == -1);

    public string SkipReason => $"This test is running on {RuntimeInformation.OSArchitecture} which is marked as to be skipped.";
}
