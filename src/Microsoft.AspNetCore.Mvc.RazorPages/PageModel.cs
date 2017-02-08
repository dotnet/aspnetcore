// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public abstract class PageModel
    {
        private PageArgumentBinder _binder;

        public PageArgumentBinder Binder
        {
            get
            {
                if (_binder == null)
                {
                    _binder = PageContext.HttpContext.RequestServices.GetRequiredService<PageArgumentBinder>();
                }

                return _binder;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _binder = value;
            }
        }

        public Page Page => PageContext.Page;

        [PageContext]
        public PageContext PageContext { get; set; }

        public ModelStateDictionary ModelState => PageContext.ModelState;

        public ViewDataDictionary ViewData => PageContext?.ViewData;

        protected Task<T> BindAsync<T>(string name)
        {
            return Binder.BindModelAsync<T>(PageContext, name);
        }

        protected Task<T> BindAsync<T>(T @default, string name)
        {
            return Binder.BindModelAsync<T>(PageContext, @default, name);
        }

        protected Task<bool> TryUpdateModelAsync<T>(T value)
        {
            return Binder.TryUpdateModelAsync<T>(PageContext, value);
        }

        protected Task<bool> TryUpdateModelAsync<T>(T value, string name)
        {
            return Binder.TryUpdateModelAsync<T>(PageContext, value, name);
        }

        protected IActionResult Redirect(string url)
        {
            return new RedirectResult(url);
        }

        protected IActionResult View()
        {
            return new PageViewResult(Page);
        }
    }
}