// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Testing.Tests
{
    public class FlakyAttributeTest
    {
        [Fact(Skip = "These tests are nice when you need them but annoying when on all the time.")]
        [Flaky("http://example.com", FlakyOn.All)]
        public void AlwaysFlakyInCI()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AGENT_OS")))
            {
                throw new Exception("Flaky!");
            }
        }

        [Fact(Skip = "These tests are nice when you need them but annoying when on all the time.")]
        [Flaky("http://example.com", FlakyOn.Helix.All)]
        public void FlakyInHelixOnly()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX")))
            {
                throw new Exception("Flaky on Helix!");
            }
        }

        [Fact(Skip = "These tests are nice when you need them but annoying when on all the time.")]
        [Flaky("http://example.com", FlakyOn.Helix.macOS1012Amd64, FlakyOn.Helix.Fedora28Amd64)]
        public void FlakyInSpecificHelixQueue()
        {
            // Today we don't run Extensions tests on Helix, but this test should light up when we do.
            var queueName = Environment.GetEnvironmentVariable("HELIX");
            if (!string.IsNullOrEmpty(queueName))
            {
                var failingQueues = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { HelixQueues.macOS1012Amd64, HelixQueues.Fedora28Amd64 };
                if (failingQueues.Contains(queueName))
                {
                    throw new Exception($"Flaky on Helix Queue '{queueName}' !");
                }
            }
        }

        [Fact(Skip = "These tests are nice when you need them but annoying when on all the time.")]
        [Flaky("http://example.com", FlakyOn.AzP.All)]
        public void FlakyInAzPOnly()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AGENT_OS")))
            {
                throw new Exception("Flaky on AzP!");
            }
        }

        [Fact(Skip = "These tests are nice when you need them but annoying when on all the time.")]
        [Flaky("http://example.com", FlakyOn.AzP.Windows)]
        public void FlakyInAzPWindowsOnly()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("AGENT_OS"), "Windows_NT"))
            {
                throw new Exception("Flaky on AzP Windows!");
            }
        }

        [Fact(Skip = "These tests are nice when you need them but annoying when on all the time.")]
        [Flaky("http://example.com", FlakyOn.AzP.macOS)]
        public void FlakyInAzPmacOSOnly()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("AGENT_OS"), "Darwin"))
            {
                throw new Exception("Flaky on AzP macOS!");
            }
        }

        [Fact(Skip = "These tests are nice when you need them but annoying when on all the time.")]
        [Flaky("http://example.com", FlakyOn.AzP.Linux)]
        public void FlakyInAzPLinuxOnly()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("AGENT_OS"), "Linux"))
            {
                throw new Exception("Flaky on AzP Linux!");
            }
        }

        [Fact(Skip = "These tests are nice when you need them but annoying when on all the time.")]
        [Flaky("http://example.com", FlakyOn.AzP.Linux, FlakyOn.AzP.macOS)]
        public void FlakyInAzPNonWindowsOnly()
        {
            var agentOs = Environment.GetEnvironmentVariable("AGENT_OS");
            if (string.Equals(agentOs, "Linux") || string.Equals(agentOs, "Darwin"))
            {
                throw new Exception("Flaky on AzP non-Windows!");
            }
        }
    }
}
