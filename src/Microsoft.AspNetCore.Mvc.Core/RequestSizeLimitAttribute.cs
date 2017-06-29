// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Sets the request body size limit to the specified size.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequestSizeLimitAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        private readonly long _bytes;

        /// <summary>
        /// Creates a new instance of <see cref="RequestSizeLimitAttribute"/>.
        /// </summary>
        /// <param name="bytes">The request body size limit.</param>
        public RequestSizeLimitAttribute(long bytes)
        {
            _bytes = bytes;
        }

        /// <inheritdoc />
        public int Order { get; set; }

        /// <inheritdoc />
        public bool IsReusable => true;

        /// <inheritdoc />
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var filter = serviceProvider.GetRequiredService<RequestSizeLimitResourceFilter>();
            filter.Bytes = _bytes;
            return filter;
        }
    }
}
