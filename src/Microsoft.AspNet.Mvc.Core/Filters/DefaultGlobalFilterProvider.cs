// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.Filters
{
    /// <summary>
    /// An implementation of <see cref="IGlobalFilterProvider"/> based on <see cref="MvcOptions"/>.
    /// </summary>
    public class DefaultGlobalFilterProvider : IGlobalFilterProvider
    {
        private readonly IReadOnlyList<IFilter> _filters;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultGlobalFilterProvider"/>.
        /// </summary>
        /// <param name="optionsAccessor">The options accessor for <see cref="MvcOptions"/>.</param>
        public DefaultGlobalFilterProvider(IOptions<MvcOptions> optionsAccessor)
        {
            var filters = optionsAccessor.Options.Filters;
            _filters = filters.ToList();
        }

        /// <inheritdoc />
        public IReadOnlyList<IFilter> Filters
        {
            get
            {
                return _filters;
            }
        }
    }
}
