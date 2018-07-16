// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    // This is an unfortunate workaround due to https://github.com/aspnet/Razor/issues/2482
    // The Razor tooling looks for a type with exactly this name and will prevent tag helper
    // discovery if it is not found.
    internal class ITagHelper
    {
    }
}
