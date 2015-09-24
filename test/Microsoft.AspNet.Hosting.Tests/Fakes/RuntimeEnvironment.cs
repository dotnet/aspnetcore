// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Dnx.Runtime;

namespace Microsoft.AspNet.Hosting.Fakes
{
    public class RuntimeEnvironment : IRuntimeEnvironment
    {
        public string OperatingSystem { get; } = "TestOs";

        public string OperatingSystemVersion { get; } = "TestOsVersion";

        public string RuntimeArchitecture { get; } = "TestArch";

        public string RuntimeType { get; } = "TestRuntime";

        public string RuntimeVersion { get; } = "TestRuntimeVersion";
    }
}
