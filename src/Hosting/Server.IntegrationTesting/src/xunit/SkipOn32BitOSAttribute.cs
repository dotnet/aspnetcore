// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    /// <summary>
    /// Skips a 64 bit test if the current OS is 32-bit.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipOn32BitOSAttribute : Attribute, ITestCondition
    {
        public bool IsMet =>
            RuntimeInformation.OSArchitecture == Architecture.Arm64
            || RuntimeInformation.OSArchitecture == Architecture.X64;

        public string SkipReason => "Skipping the x64 test since Windows is 32-bit";
    }
}