// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.AspNetCore.Server.Testing
{
    /// <summary>
    /// Skip test if IIS variations are not enabled. To enable set environment variable 
    /// IIS_VARIATIONS_ENABLED=true for the test process.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfIISVariationsNotEnabledAttribute : Attribute, ITestCondition
    {
        public bool IsMet
        {
            get
            {
                return Environment.GetEnvironmentVariable("IIS_VARIATIONS_ENABLED") == "true";
            }
        }

        public string SkipReason
        {
            get
            {
                return "Skipping IIS variation of tests. " +
                        "To run the IIS variations, setup IIS and set the environment variable IIS_VARIATIONS_ENABLED=true.";
            }
        }
    }
}