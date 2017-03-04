// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// A base class for a Razor page.
    /// </summary>
    public abstract class Page : RazorPageBase, IRazorPage
    {
        private PageArgumentBinder _binder;

        /// <summary>
        /// The <see cref="RazorPages.PageContext"/>.
        /// </summary>
        public PageContext PageContext { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="PageArgumentBinder"/>.
        /// </summary>
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

        /// <summary>
        /// Gets the <see cref="ModelStateDictionary"/>.
        /// </summary>
        public ModelStateDictionary ModelState => PageContext?.ModelState;

        /// <summary>
        /// Gets the <see cref="ITempDataDictionary"/> from the <see cref="PageContext"/>.
        /// </summary>
        /// <remarks>Returns null if <see cref="PageContext"/> is null.</remarks>
        public ITempDataDictionary TempData => PageContext?.TempData;

        /// <inheritdoc />
        public override void EnsureRenderedBodyOrSections()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void BeginContext(int position, int length, bool isLiteral)
        {
            const string BeginContextEvent = "Microsoft.AspNetCore.Mvc.Razor.BeginInstrumentationContext";

            if (DiagnosticSource?.IsEnabled(BeginContextEvent) == true)
            {
                DiagnosticSource.Write(
                    BeginContextEvent,
                    new
                    {
                        httpContext = ViewContext,
                        path = Path,
                        position = position,
                        length = length,
                        isLiteral = isLiteral,
                    });
            }
        }

        /// <inheritdoc />
        public override void EndContext()
        {
            const string EndContextEvent = "Microsoft.AspNetCore.Mvc.Razor.EndInstrumentationContext";

            if (DiagnosticSource?.IsEnabled(EndContextEvent) == true)
            {
                DiagnosticSource.Write(
                    EndContextEvent,
                    new
                    {
                        httpContext = ViewContext,
                        path = Path,
                    });
            }
        }

        /// <summary>
        /// Creates a <see cref="RedirectResult"/> object that redirects to the specified <paramref name="url"/>.
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <returns>The created <see cref="RedirectResult"/> for the response.</returns>
        protected RedirectResult Redirect(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(url));
            }

            return new RedirectResult(url);
        }

        /// <summary>
        /// Creates a <see cref="PageViewResult"/> object that renders this page as a view to the response.
        /// </summary>
        /// <returns>The created <see cref="PageViewResult"/> object for the response.</returns>
        /// <remarks>
        /// Returning a <see cref="PageViewResult"/> from a page handler method is equivalent to returning void.
        /// The view associated with the page will be executed.
        /// </remarks>
        protected PageViewResult View()
        {
            return new PageViewResult(this);
        }
    }
}
