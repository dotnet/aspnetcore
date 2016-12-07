// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PageResultExecutor
    {
        public virtual Task ExecuteAsync(PageContext pageContext, PageViewResult result)
        {
            if (result.Model != null)
            {
                result.Page.PageContext.ViewData.Model = result.Model;
            }

            throw new NotImplementedException();
        }
    }
}
