// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Skip test if running on helix (or a particular helix queue).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SkipOnHelixAttribute : Attribute, ITestCondition
    {
        public SkipOnHelixAttribute(string issueUrl)
        {
            if (string.IsNullOrEmpty(issueUrl))
            {
                throw new ArgumentNullException(nameof(issueUrl));
            }
            IssueUrl = issueUrl;
        }

        public string IssueUrl { get; }

        public bool IsMet
        {
            get
            {
                var skip = OnHelix() && ShouldSkip();
                return !skip;
            }
        }

        // Queues that should be skipped on, i.e. "Windows.10.Amd64.ClientRS4.VS2017.Open;OSX.1012.Amd64.Open"
        public string Queues { get; set; }

        public string SkipReason
        {
            get
            {
                return $"This test is skipped on helix";
            }
        }

        private bool ShouldSkip()
        {
            if (Queues == null)
            {
                return true;
            }

            var targetQueue = GetTargetHelixQueue().ToLowerInvariant();

            if (Queues.Contains("All.OSX") && targetQueue.StartsWith("osx", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return Queues.ToLowerInvariant().Split(';').Contains(targetQueue);
        }

        public static bool OnHelix() => !string.IsNullOrEmpty(GetTargetHelixQueue());

        public static string GetTargetHelixQueue() => Environment.GetEnvironmentVariable("helix");
    }
}
