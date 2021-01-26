// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis.Razor
{
    [Flags]
    internal enum TagHelperDiscoveryFilter
    {
        CurrentCompilation = 1,
        ReferenceAssemblies = 2,
        Default = CurrentCompilation | ReferenceAssemblies,
    };
}
