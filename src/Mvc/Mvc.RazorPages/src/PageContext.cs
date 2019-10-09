// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// The context associated with the current request for a Razor page.
    /// </summary>
    public class PageContext : ActionContext
    {
        private CompiledPageActionDescriptor _actionDescriptor;
        private IList<IValueProviderFactory> _valueProviderFactories;
        private ViewDataDictionary _viewData;
        private IList<Func<IRazorPage>> _viewStartFactories;

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
        public PageContext(ActionContext actionContext)
            : base(actionContext)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="PageActionDescriptor"/>.
        /// </summary>
        public virtual new CompiledPageActionDescriptor ActionDescriptor
        {
            get => _actionDescriptor;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _actionDescriptor = value;
                base.ActionDescriptor = value;
            }
        }

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

        /// <summary>
        /// Gets or sets <see cref="ViewDataDictionary"/>.
        /// </summary>
        public virtual ViewDataDictionary ViewData
        {
            get => _viewData;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _viewData = value;
            }
        }

        /// <summary>
        /// Gets or sets the applicable _ViewStart instances.
        /// </summary>
        public virtual IList<Func<IRazorPage>> ViewStartFactories
        {
            get => _viewStartFactories;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _viewStartFactories = value;
            }
        }
    }
}