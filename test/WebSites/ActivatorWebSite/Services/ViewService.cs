// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;

namespace ActivatorWebSite
{
    /// <summary>
    /// A service that needs to be contextualized.
    /// </summary>
    public class ViewService : ICanHasViewContext
    {
        private ViewContext _context;

        public void Contextualize(ViewContext viewContext)
        {
            _context = viewContext;    
        }

        public string GetValue()
        {
            return _context.HttpContext.Request.Query["test"];
        }
    }
}