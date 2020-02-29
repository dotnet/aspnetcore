// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfDockerNotPresentAttribute : Attribute, ITestCondition
    {
        public bool IsMet => CheckDocker();
        public string SkipReason { get; private set; } = "Docker is not available";

        private bool CheckDocker()
        {
            if (Docker.Default != null)
            {
                // Docker is present, but is it working?
                if (Docker.Default.RunCommand("ps", "docker ps", out var output) != 0)
                {
                    SkipReason = $"Failed to invoke test command 'docker ps'. Output: {output}";
                }
                else
                {
                    // We have a docker
                    return true;
                }
            }
            else
            {
                SkipReason = "Docker is not installed on the host machine.";
            }

            // If we get here, we don't have a docker
            return false;
        }
    }
}
