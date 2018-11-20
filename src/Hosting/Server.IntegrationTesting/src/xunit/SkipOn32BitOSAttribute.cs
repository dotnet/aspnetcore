// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    /// <summary>
    /// Skips a 64 bit test if the current Windows OS is 32-bit.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipOn32BitOSAttribute : Attribute, ITestCondition
    {
        public bool IsMet
        {
            get
            {
                // Directory found only on 64-bit OS.
                return Directory.Exists(Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), "SysWOW64"));
            }
        }

        public string SkipReason
        {
            get
            {
                return "Skipping the x64 test since Windows is 32-bit";
            }
        }
    }
}