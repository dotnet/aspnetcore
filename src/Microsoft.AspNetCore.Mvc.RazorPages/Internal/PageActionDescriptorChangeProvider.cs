// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageActionDescriptorChangeProvider : IActionDescriptorChangeProvider
    {
        private readonly RazorProject _razorProject;

        public PageActionDescriptorChangeProvider(RazorProject razorProject)
        {
            _razorProject = razorProject;
        }

        public IChangeToken GetChangeToken() => ((DefaultRazorProject)_razorProject).Watch("**/*.cshtml");
    }
}
