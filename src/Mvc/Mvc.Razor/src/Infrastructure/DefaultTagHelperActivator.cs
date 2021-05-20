// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Razor.Infrastructure
{
    /// <summary>
    /// Default implementation of <see cref="ITagHelperActivator"/>.
    /// </summary>
    internal class DefaultTagHelperActivator : ITagHelperActivator
    {
        /// <inheritdoc />
        public TTagHelper Create<TTagHelper>(ViewContext context)
            where TTagHelper : ITagHelper
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Cache<TTagHelper>.Create(context.HttpContext.RequestServices);
        }

        private static class Cache<TTagHelper>
        {
            private static readonly ObjectFactory _objectFactory = ActivatorUtilities.CreateFactory(typeof(TTagHelper), Type.EmptyTypes);

            public static TTagHelper Create(IServiceProvider serviceProvider) => (TTagHelper)_objectFactory(serviceProvider, arguments: null);
        }
    }
}
