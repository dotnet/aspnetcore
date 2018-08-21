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
    //
    // This has to be its own assembly because we need to reference it in Blazor component libraries
    // in order for component discovery to work, but if we allow it as a reference for server-side
    // projects it will break MVC's features.
    internal class ITagHelper
    {
    }
}
