// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Executes a middleware pipeline provided the by the <see cref="MiddlewareFilterAttribute.ConfigurationType"/>.
    /// The middleware pipeline will be treated as an async resource filter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class MiddlewareFilterAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        /// <summary>
        /// Instantiates a new instance of <see cref="MiddlewareFilterAttribute"/>.
        /// </summary>
        /// <param name="configurationType">A type which configures a middleware pipeline.</param>
        public MiddlewareFilterAttribute(Type configurationType)
        {
            if (configurationType == null)
            {
                throw new ArgumentNullException(nameof(configurationType));
            }

            ConfigurationType = configurationType;
        }

        public Type ConfigurationType { get; }

        /// <inheritdoc />
        public int Order { get; set; }

        /// <inheritdoc />
        public bool IsReusable => true;

        /// <inheritdoc />
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var middlewarePipelineService = serviceProvider.GetRequiredService<MiddlewareFilterBuilder>();
            var pipeline = middlewarePipelineService.GetPipeline(ConfigurationType);

            return new MiddlewareFilter(pipeline);
        }
    }
}
