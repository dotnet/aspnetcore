// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageFilterApplicationModelProvider : IPageApplicationModelProvider
    {
        /// <remarks>This order ensures that <see cref="PageFilterApplicationModelProvider"/> runs after 
        /// <see cref="RazorProjectPageApplicationModelProvider"/> and <see cref="CompiledPageApplicationModelProvider"/>.
        /// </remarks>
        public int Order => -1000 + 10;

        public void OnProvidersExecuted(PageApplicationModelProviderContext context)
        {
            // Do nothing
        }

        public void OnProvidersExecuting(PageApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            for (var i = 0; i < context.Results.Count; i++)
            {
                var pageApplicationModel = context.Results[i];
            
                // Support for [TempData] on properties
                pageApplicationModel.Filters.Add(new PageSaveTempDataPropertyFilterFactory());

                // Always require an antiforgery token on post
                pageApplicationModel.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            }
        }
    }
}
