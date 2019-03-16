using Microsoft.AspNetCore.Testing.xunit;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Testing.Tests
{
    public class FlakyAttributeTest
    {
        [Fact]
        [Flaky("http://example.com")]
        public void AlwaysFlaky()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AGENT_OS")))
            {
                throw new Exception("Flaky!");
            }
        }

        [Fact]
        [Flaky("http://example.com", HelixQueues.All)]
        public void FlakyInHelixOnly()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX")))
            {
                throw new Exception("Flaky on Helix!");
            }
        }

        [Fact]
        [Flaky("http://example.com", HelixQueues.macOS1012Amd64, HelixQueues.Fedora28Amd64)]
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

        [Fact]
        [Flaky("http://example.com", AzurePipelines.All)]
        public void FlakyInAzPOnly()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AGENT_OS")))
            {
                throw new Exception("Flaky on AzP!");
            }
        }

        [Fact]
        [Flaky("http://example.com", AzurePipelines.Windows)]
        public void FlakyInAzPWindowsOnly()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("AGENT_OS"), AzurePipelines.Windows))
            {
                throw new Exception("Flaky on AzP Windows!");
            }
        }

        [Fact]
        [Flaky("http://example.com", AzurePipelines.macOS)]
        public void FlakyInAzPmacOSOnly()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("AGENT_OS"), AzurePipelines.macOS))
            {
                throw new Exception("Flaky on AzP macOS!");
            }
        }

        [Fact]
        [Flaky("http://example.com", AzurePipelines.Linux)]
        public void FlakyInAzPLinuxOnly()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("AGENT_OS"), AzurePipelines.Linux))
            {
                throw new Exception("Flaky on AzP Linux!");
            }
        }

        [Fact]
        [Flaky("http://example.com", AzurePipelines.Linux, AzurePipelines.macOS)]
        public void FlakyInAzPNonWindowsOnly()
        {
            var agentOs = Environment.GetEnvironmentVariable("AGENT_OS");
            if (string.Equals(agentOs, "Linux") || string.Equals(agentOs, AzurePipelines.macOS))
            {
                throw new Exception("Flaky on AzP non-Windows!");
            }
        }
    }
}
