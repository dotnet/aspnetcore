// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PassThruRazorPageActivator : IRazorPageActivator
    {
        private readonly IRazorPageActivator _pageActivator;

        public PassThruRazorPageActivator(IRazorPageActivator pageActivator)
        {
            _pageActivator = pageActivator;
        }

        public void Activate(IRazorPage page, ViewContext context)
        {
            var razorView = (RazorView)context.View;
            if (ReferenceEquals(page, razorView.RazorPage))
            {
                return;
            }

            _pageActivator.Activate(page, context);
        }
    }
}
