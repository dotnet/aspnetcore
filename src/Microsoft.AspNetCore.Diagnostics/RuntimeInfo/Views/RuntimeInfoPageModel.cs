// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Diagnostics.Views
{
    public class RuntimeInfoPageModel
    {
        public string Version { get; internal set; }

        public string OperatingSystem { get; internal set; }

        public string RuntimeArchitecture { get; internal set; }

        public string RuntimeType { get; internal set; }
    }
}