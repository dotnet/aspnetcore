// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A context for resource filters. Allows modification of services and values used for
    /// model binding.
    /// </summary>
    public class ResourceExecutingContext : FilterContext
    {
        /// <summary>
        /// Creates a new <see cref="ResourceExecutingContext"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
        /// <param name="filters">The list of <see cref="IFilter"/> instances.</param>
        public ResourceExecutingContext(ActionContext actionContext, IList<IFilter> filters)
            : base(actionContext, filters)
        {
        }

        /// <summary>
        /// Gets or sets the list of <see cref="IInputFormatter"/> instances used by model binding.
        /// </summary>
        public virtual IList<IInputFormatter> InputFormatters { get; set; }

        /// <summary>
        /// Gets or sets the list of <see cref="IModelBinder"/> instances used by model binding.
        /// </summary>
        public virtual IList<IModelBinder> ModelBinders { get; set; }

        /// <summary>
        /// Gets or sets the result of the action to be executed. 
        /// </summary>
        /// <remarks>
        /// Setting <see cref="Result"/> to a non-<c>null</c> value inside a resource filter will
        /// short-circuit execution of additional resource filtes and the action itself.
        /// </remarks>
        public virtual IActionResult Result { get; set; }

        /// <summary>
        /// Gets or sets the list of <see cref="IValueProviderFactory"/> instances used by model binding.
        /// </summary>
        public IList<IValueProviderFactory> ValueProviderFactories { get; set; }

        /// <summary>
        /// Gets or sets the list of <see cref="IModelValidatorProvider"/> instances used by model binding.
        /// </summary>
        public IList<IModelValidatorProvider> ValidatorProviders { get; set; }
    }
}