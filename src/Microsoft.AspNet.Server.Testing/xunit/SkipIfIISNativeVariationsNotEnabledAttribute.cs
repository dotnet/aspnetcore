// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Testing.xunit;

namespace Microsoft.AspNet.Server.Testing
{
    /// <summary>
    /// Skip test if IIS native module is not enabled. To enable setup native module
    /// and set environment variable IIS_NATIVE_VARIATIONS_ENABLED=true for the test process.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfIISNativeVariationsNotEnabledAttribute : Attribute, ITestCondition
    {
        public bool IsMet
        {
            get
            {
                return Environment.GetEnvironmentVariable("IIS_NATIVE_VARIATIONS_ENABLED") == "true";
            }
        }

        public string SkipReason
        {
            get
            {
                return "Skipping Native module test since native module variations are not enabled. " +
                        "To run the test, setup the native module and set the environment variable IIS_NATIVE_VARIATIONS_ENABLED=true.";
            }
        }
    }
}