// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <summary>
    /// Represents the properties and methods that are needed in order to render a view that uses Razor syntax.
    /// </summary>
    /// <typeparam name="TModel">The type of the view data model.</typeparam>
    public abstract class RazorPage<TModel> : RazorPage
    {
        /// <summary>
        /// Gets the Model property of the <see cref="ViewData"/> property.
        /// </summary>
        public TModel Model => ViewData == null ? default(TModel) : ViewData.Model;

        /// <summary>
        /// Gets or sets the dictionary for view data.
        /// </summary>
        [RazorInject]
        public ViewDataDictionary<TModel> ViewData { get; set; }

    }
}
