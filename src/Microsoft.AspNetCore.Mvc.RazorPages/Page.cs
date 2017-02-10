// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// A base class for a Razor page.
    /// </summary>
    public abstract class Page : RazorPageBase, IRazorPage
    {
        private IUrlHelper _urlHelper;
        private PageArgumentBinder _binder;

        /// <summary>
        /// The <see cref="PageContext"/>.
        /// </summary>
        public PageContext PageContext { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="System.Diagnostics.DiagnosticSource"/> instance used to instrument the page execution.
        /// </summary>
        [RazorInject]
        public DiagnosticSource DiagnosticSource { get; set; }

        /// <summary>
        /// Gets the <see cref="System.Text.Encodings.Web.HtmlEncoder"/> to use when this <see cref="RazorPage"/>
        /// handles non-<see cref="IHtmlContent"/> C# expressions.
        /// </summary>
        [RazorInject]
        public HtmlEncoder HtmlEncoder
        {
            get { return Encoder; }
            set { Encoder = value; }
        }

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

        /// <inheritdoc />
        public override void EnsureRenderedBodyOrSections()
        {
            throw new NotSupportedException();
        }

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

    }
}
