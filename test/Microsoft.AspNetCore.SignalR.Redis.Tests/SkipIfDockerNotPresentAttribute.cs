// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.AspNetCore.SignalR.Redis.Tests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SkipIfDockerNotPresentAttribute : Attribute, ITestCondition
    {
        public bool IsMet => CheckDocker();
        public string SkipReason { get; private set; } = "Docker is not available";
        public string RequiredOsType { get; }

        public SkipIfDockerNotPresentAttribute() : this("linux")
        {

        }

        public SkipIfDockerNotPresentAttribute(string requiredOSType) 
        {
            RequiredOsType = requiredOSType;
        }

        private bool CheckDocker()
        {
            if(Docker.Default != null)
            {
                // Docker is present, but is it working?
                if (Docker.Default.RunCommand("info -f {{.OSType}}", out var output) != 0)
                {
                    SkipReason = $"Failed to invoke test command 'docker ps'. Output: {output}";
                }
                else if (!string.Equals(output.Trim(), RequiredOsType, StringComparison.Ordinal))
                {
                    SkipReason = $"Docker tests do not support the OS type '{output.Trim()}', they require '{RequiredOsType}'.";
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
