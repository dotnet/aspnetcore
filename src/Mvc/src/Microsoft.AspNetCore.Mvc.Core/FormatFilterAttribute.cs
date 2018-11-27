// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A filter that will use the format value in the route data or query string to set the content type on an
    /// <see cref="ObjectResult" /> returned from an action.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class FormatFilterAttribute : Attribute, IFilterFactory
    {
        /// <inheritdoc />
        public bool IsReusable => true;

        /// <summary>
        /// Creates an instance of <see cref="FormatFilter"/>.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        /// <returns>An instance of <see cref="FormatFilter"/>.</returns>
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            return serviceProvider.GetRequiredService<FormatFilter>();
        }
    }
}