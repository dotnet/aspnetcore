// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    public abstract class PageModel
    {
        private PageArgumentBinder _binder;
        private IUrlHelper _urlHelper;

        /// <summary>
        /// Gets or sets the <see cref="PageArgumentBinder"/>.
        /// </summary>
        public PageArgumentBinder Binder
        {
            get
            {
                if (_binder == null)
                {
                    _binder = HttpContext?.RequestServices?.GetRequiredService<PageArgumentBinder>();
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

        /// <summary>
        /// Gets or sets the <see cref="IUrlHelper"/>.
        /// </summary>
        public IUrlHelper Url
        {
            get
            {
                if (_urlHelper == null)
                {
                    var factory = HttpContext?.RequestServices?.GetRequiredService<IUrlHelperFactory>();
                    _urlHelper = factory?.GetUrlHelper(PageContext);
                }

                return _urlHelper;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _urlHelper = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="RazorPages.Page"/> instance this model belongs to.
        /// </summary>
        public Page Page => PageContext?.Page;

        /// <summary>
        /// Gets the <see cref="RazorPages.PageContext"/>.
        /// </summary>
        [PageContext]
        public PageContext PageContext { get; set; }

        /// <summary>
        /// Gets the <see cref="ViewContext"/>.
        /// </summary>
        public ViewContext ViewContext => PageContext;

        /// <summary>
        /// Gets the <see cref="Http.HttpContext"/>.
        /// </summary>
        public HttpContext HttpContext => PageContext?.HttpContext;

        /// <summary>
        /// Gets the <see cref="HttpRequest"/>.
        /// </summary>
        public HttpRequest Request => HttpContext?.Request;

        /// <summary>
        /// Gets the <see cref="HttpResponse"/>.
        /// </summary>
        public HttpResponse Response => HttpContext?.Response;

        /// <summary>
        /// Gets the <see cref="ModelStateDictionary"/>.
        /// </summary>
        public ModelStateDictionary ModelState => PageContext.ModelState;

        /// <summary>
        /// Gets the <see cref="ITempDataDictionary"/> from the <see cref="PageContext"/>.
        /// </summary>
        /// <remarks>Returns null if <see cref="PageContext"/> is null.</remarks>
        public ITempDataDictionary TempData => PageContext?.TempData;

        /// <summary>
        /// Gets the <see cref="ViewDataDictionary"/>.
        /// </summary>
        public ViewDataDictionary ViewData => PageContext?.ViewData;

        /// <summary>
        /// Binds the model with the specified <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="name">The model name.</param>
        /// <returns>A <see cref="Task"/> that on completion returns the bound model.</returns>
        protected internal Task<TModel> BindAsync<TModel>(string name)
        {
            return Binder.BindModelAsync<TModel>(PageContext, name);
        }

        /// <summary>
        /// Binds the model with the specified <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="name">The model name.</param>
        /// <param name="default">The default model value.</param>
        /// <returns>A <see cref="Task"/> that on completion returns the bound model.</returns>
        protected internal Task<TModel> BindAsync<TModel>(TModel @default, string name)
        {
            return Binder.BindModelAsync(PageContext, @default, name);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using values from the controller's current
        /// <see cref="IValueProvider"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful.</returns>
        protected internal Task<bool> TryUpdateModelAsync<TModel>(TModel model)
        {
            return Binder.TryUpdateModelAsync(PageContext, model);
        }

        /// <summary>
        /// Updates the specified <paramref name="model"/> instance using values from the controller's current
        /// <see cref="IValueProvider"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model object.</typeparam>
        /// <param name="model">The model instance to update.</param>
        /// <param name="name">The model name.</param>
        /// <returns>A <see cref="Task"/> that on completion returns <c>true</c> if the update is successful.</returns>
        protected internal Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name)
        {
            return Binder.TryUpdateModelAsync(PageContext, model, name);
        }

        /// <summary>
        /// Creates a <see cref="RedirectResult"/> object that redirects (<see cref="StatusCodes.Status302Found"/>)
        /// to the specified <paramref name="url"/>.
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <returns>The created <see cref="RedirectResult"/> for the response.</returns>
        protected internal RedirectResult Redirect(string url)
        {
            return new RedirectResult(url);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the current page.
        /// </summary>
        /// <returns>The <see cref="RedirectToPageResult"/>.</returns>
        protected RedirectToPageResult RedirectToPage()
            => RedirectToPage(pageName: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the current page with the specified <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The <see cref="RedirectToPageResult"/>.</returns>
        protected RedirectToPageResult RedirectToPage(object routeValues)
            => RedirectToPage(pageName: null, routeValues: routeValues);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <returns>The <see cref="RedirectToPageResult"/>.</returns>
        protected internal RedirectToPageResult RedirectToPage(string pageName)
            => RedirectToPage(pageName, routeValues: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The <see cref="RedirectToPageResult"/>.</returns>
        protected internal RedirectToPageResult RedirectToPage(string pageName, object routeValues)
            => RedirectToPage(pageName, routeValues, fragment: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="fragment"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The <see cref="RedirectToPageResult"/>.</returns>
        protected internal RedirectToPageResult RedirectToPage(string pageName, string fragment)
            => RedirectToPage(pageName, routeValues: null, fragment: fragment);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="routeValues"/> and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The <see cref="RedirectToPageResult"/>.</returns>
        protected internal RedirectToPageResult RedirectToPage(string pageName, object routeValues, string fragment)
            => new RedirectToPageResult(pageName, routeValues, fragment);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified <paramref name="pageName"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <returns>The <see cref="RedirectToPageResult"/> with <see cref="RedirectToPageResult.Permanent"/> set.</returns>
        protected internal RedirectToPageResult RedirectToPagePermanent(string pageName)
            => RedirectToPagePermanent(pageName, routeValues: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The <see cref="RedirectToPageResult"/> with <see cref="RedirectToPageResult.Permanent"/> set.</returns>
        protected internal RedirectToPageResult RedirectToPagePermanent(string pageName, object routeValues)
            => RedirectToPagePermanent(pageName, routeValues, fragment: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="fragment"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The <see cref="RedirectToPageResult"/> with <see cref="RedirectToPageResult.Permanent"/> set.</returns>
        protected internal RedirectToPageResult RedirectToPagePermanent(string pageName, string fragment)
            => RedirectToPagePermanent(pageName, routeValues: null, fragment: fragment);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified <paramref name="pageName"/>
        /// using the specified <paramref name="routeValues"/> and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="pageName">The name of the page.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The <see cref="RedirectToPageResult"/> with <see cref="RedirectToPageResult.Permanent"/> set.</returns>
        protected internal RedirectToPageResult RedirectToPagePermanent(string pageName, object routeValues, string fragment)
            => new RedirectToPageResult(pageName, routeValues, permanent: true, fragment: fragment);

        /// <summary>
        /// Creates a <see cref="PageViewResult"/> object that renders the page.
        /// </summary>
        /// <returns>The <see cref="PageViewResult"/>.</returns>
        protected internal PageViewResult View()
        {
            return new PageViewResult(Page);
        }
    }
}