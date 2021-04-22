// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Skip test if running on CI
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SkipOnCIAttribute : Attribute, ITestCondition
    {
        public SkipOnCIAttribute(string issueUrl = "")
        {
            IssueUrl = issueUrl;
        }

        public string IssueUrl { get; }

        public bool IsMet
        {
            get
            {
                return !OnCI();
            }
        }

        public string SkipReason
        {
            get
            {
                return $"This test is skipped on CI";
            }
        }

        public static bool OnCI() => OnHelix() || OnAzdo();
        public static bool OnHelix() => !string.IsNullOrEmpty(GetTargetHelixQueue());
        public static string GetTargetHelixQueue() => Environment.GetEnvironmentVariable("helix");
        public static bool OnAzdo() => !string.IsNullOrEmpty(GetIfOnAzdo());
        public static string GetIfOnAzdo() => Environment.GetEnvironmentVariable("AGENT_OS");
    }
}
