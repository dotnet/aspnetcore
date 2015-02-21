// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A filter which will use the format value in the route data or query string to set the content type on an 
    /// <see cref="ObjectResult" /> returned from an action.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class FormatFilterAttribute : Attribute, IFilterFactory
    {
        /// <summary>
        /// Creates an instance of <see cref="FormatFilter"/>
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider "/></param>
        /// <returns>An instance of <see cref="FormatFilter"/></returns>
        public IFilter CreateInstance([NotNull] IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<FormatFilter>();
        }
    }
}