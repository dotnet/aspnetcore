// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// The context associated with the current request for a Razor page.
    /// </summary>
    public class PageContext : ViewContext
    {
        private CompiledPageActionDescriptor _actionDescriptor;
        private Page _page;
        private IList<IValueProviderFactory> _valueProviderFactories;

        /// <summary>
        /// Creates an empty <see cref="PageContext"/>.
        /// </summary>
        /// <remarks>
        /// The default constructor is provided for unit test purposes only.
        /// </remarks>
        public PageContext()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PageContext"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
        /// <param name="viewData">The <see cref="ViewDataDictionary"/>.</param>
        /// <param name="tempDataDictionary">The <see cref="ITempDataDictionary"/>.</param>
        /// <param name="htmlHelperOptions">The <see cref="HtmlHelperOptions"/> to apply to this instance.</param>
        public PageContext(
            ActionContext actionContext,
            ViewDataDictionary viewData,
            ITempDataDictionary tempDataDictionary,
            HtmlHelperOptions htmlHelperOptions)
            : base(actionContext, NullView.Instance, viewData, tempDataDictionary, TextWriter.Null, htmlHelperOptions)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="PageActionDescriptor"/>.
        /// </summary>
        public new CompiledPageActionDescriptor ActionDescriptor
        {
            get
            {
                return _actionDescriptor;
            }
            set
            {
                _actionDescriptor = value;
                base.ActionDescriptor = value;
            }
        }

        public Page Page
        {
            get { return _page; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _page = value;
            }
        }

        /// <summary>
        /// Gets or sets the applicable _ViewStart instances.
        /// </summary>
        public IReadOnlyList<IRazorPage> ViewStarts { get; set; }

        /// <summary>
        /// Gets or sets the list of <see cref="IValueProviderFactory"/> instances for the current request.
        /// </summary>
        public virtual IList<IValueProviderFactory> ValueProviderFactories
        {
            get
            {
                if (_valueProviderFactories == null)
                {
                    _valueProviderFactories = new List<IValueProviderFactory>();
                }

                return _valueProviderFactories;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _valueProviderFactories = value;
            }
        }
    }
}