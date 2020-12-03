// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.AspNetCore.Testing
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class DockerOnlyAttribute : Attribute, ITestCondition
    {
        public string SkipReason { get; } = "This test can only run in a Docker container.";

        public bool IsMet
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // we currently don't have a good way to detect if running in a Windows container
                    return false;
                }

                const string procFile = "/proc/1/cgroup";
                if (!File.Exists(procFile))
                {
                    return false;
                }

                using (StreamReader sr = new StreamReader(procFile, Encoding.UTF8))
                {
                    while (sr.ReadLine() is string line)
                    {
                        if (line.EndsWith("name=openrc:/docker", StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}
