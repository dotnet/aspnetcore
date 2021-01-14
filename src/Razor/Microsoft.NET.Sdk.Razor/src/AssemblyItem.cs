// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class AssemblyItem
    {
        public string Path { get; set; }

        public bool IsFrameworkReference { get; set; }

        public string AssemblyName { get; set; }
    }
}
