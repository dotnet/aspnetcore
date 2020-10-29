// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    [Flags]
    public enum  TagHelperDiscoveryMode
    {
        CurrentAssembly = 1,
        References = 2,
        All = CurrentAssembly | References,
    }
}
