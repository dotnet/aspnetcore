// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
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

        /// <summary>
        /// Creates an empty <see cref="ViewContext"/>.
        /// </summary>
        /// <remarks>
        /// The default constructor is provided for unit test purposes only.
        /// </remarks>
        public PageContext()
        {
        }

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
    }
}