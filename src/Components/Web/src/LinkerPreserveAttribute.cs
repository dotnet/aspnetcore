// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    // There doesn't appear to be a standard "preserve type" attribute known to the linker natively,
    // so code in LinkerConfigGenerator matches this attribute by name. Any assembly in this repo
    // can have its own copy of this attribute.
    [AttributeUsage(AttributeTargets.Class)]
    internal class LinkerPreserveAttribute : Attribute
    {
    }
}
