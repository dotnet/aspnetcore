// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.AspNetCore.Server.Testing
{
    /// <summary>
    /// Skips a test if the DNX used to run the test is CoreClr.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfCurrentRuntimeIsCoreClrAttribute : Attribute, ITestCondition
    {
        public bool IsMet
        {
            get
            {
                return !Process.GetCurrentProcess().ProcessName.ToLower().Contains("coreclr");
            }
        }

        public string SkipReason
        {
            get
            {
                return "Cannot run these test variations using CoreCLR DNX as helpers are not available on CoreCLR.";
            }
        }
    }
}
