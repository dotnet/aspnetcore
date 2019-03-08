// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Represents properties and methods that are used by <see cref="RazorView"/> for execution.
    /// </summary>
    public interface IRazorPage : IRazorRazorPage<ViewContext>
    {
    }
}