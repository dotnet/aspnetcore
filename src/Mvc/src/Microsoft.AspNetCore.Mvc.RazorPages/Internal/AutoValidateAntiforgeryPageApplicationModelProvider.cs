// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class AutoValidateAntiforgeryPageApplicationModelProvider : IPageApplicationModelProvider
    {
        // The order is set to execute after the DefaultPageApplicationModelProvider.
        public int Order => -1000 + 10;

        public void OnProvidersExecuted(PageApplicationModelProviderContext context)
        {
        }

        public void OnProvidersExecuting(PageApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var pageApplicationModel = context.PageApplicationModel;
            
            // Always require an antiforgery token on post
            pageApplicationModel.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        }
    }
}
