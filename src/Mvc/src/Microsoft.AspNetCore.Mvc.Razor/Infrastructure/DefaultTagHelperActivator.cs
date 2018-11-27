// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor.Infrastructure
{
    /// <summary>
    /// Default implementation of <see cref="ITagHelperActivator"/>.
    /// </summary>
    internal class DefaultTagHelperActivator : ITagHelperActivator
    {
        private readonly ITypeActivatorCache _typeActivatorCache;

        /// <summary>
        /// Instantiates a new <see cref="DefaultTagHelperActivator"/> instance.
        /// </summary>
        /// <param name="typeActivatorCache">The <see cref="ITypeActivatorCache"/>.</param>
        public DefaultTagHelperActivator(ITypeActivatorCache typeActivatorCache)
        {
            if (typeActivatorCache == null)
            {
                throw new ArgumentNullException(nameof(typeActivatorCache));
            }

            _typeActivatorCache = typeActivatorCache;
        }

        /// <inheritdoc />
        public TTagHelper Create<TTagHelper>(ViewContext context)
            where TTagHelper : ITagHelper
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return _typeActivatorCache.CreateInstance<TTagHelper>(
                context.HttpContext.RequestServices,
                typeof(TTagHelper));
        }
    }
}
