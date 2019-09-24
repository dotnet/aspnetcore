// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    /// <summary>
    /// Skip test if a given environment variable is not enabled. To enable the test, set environment variable
    /// to "true" for the test process.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SkipIfEnvironmentVariableNotEnabledAttribute : Attribute, ITestCondition
    {
        private readonly string _environmentVariableName;

        public SkipIfEnvironmentVariableNotEnabledAttribute(string environmentVariableName)
        {
            _environmentVariableName = environmentVariableName;
        }

        public bool IsMet
        {
            get
            {
                return string.Compare(Environment.GetEnvironmentVariable(_environmentVariableName), "true", ignoreCase: true) == 0;
            }
        }

        public string SkipReason
        {
            get
            {
                return $"To run this test, set the environment variable {_environmentVariableName}=\"true\". {AdditionalInfo}";
            }
        }

        public string AdditionalInfo { get; set; }
    }
}