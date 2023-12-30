// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

public class RuntimeArchitectures
{
    public static RuntimeArchitecture Current
    {
        get
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.Arm64 => RuntimeArchitecture.arm64,
                Architecture.X64 => RuntimeArchitecture.x64,
                Architecture.X86 => RuntimeArchitecture.x86,
                Architecture.Ppc64le => RuntimeArchitecture.ppc64le,
                Architecture.S390x => RuntimeArchitecture.s390x,
                _ => throw new NotImplementedException($"Unknown RuntimeInformation.OSArchitecture: {RuntimeInformation.OSArchitecture.ToString()}"),
            };
        }
    }
}
