// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    /// <inheritdoc />
    public class DefaultValidationExcludeFiltersProvider
        : OptionDescriptorBasedProvider<IExcludeTypeValidationFilter>, IValidationExcludeFiltersProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultValidationExcludeFiltersProvider"/> class.
        /// </summary>
        /// <param name="options">An accessor to the <see cref="MvcOptions"/> configured for this application.</param>
        /// <param name="typeActivatorCache">The <see cref="ITypeActivatorCache"/> cache.</param>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        public DefaultValidationExcludeFiltersProvider(IOptions<MvcOptions> optionsAccessor,
                                                       ITypeActivatorCache typeActivatorCache,
                                                       IServiceProvider serviceProvider)
            : base(optionsAccessor.Options.ValidationExcludeFilters, typeActivatorCache, serviceProvider)
        {
        }

        /// <inheritdoc />
        public IReadOnlyList<IExcludeTypeValidationFilter> ExcludeFilters
        {
            get
            {
                return Options;
            }
        }
    }
}