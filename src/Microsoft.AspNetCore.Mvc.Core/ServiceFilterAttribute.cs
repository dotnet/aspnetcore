// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    [DebuggerDisplay("ServiceFilter: Type={ServiceType} Order={Order}")]
    public class ServiceFilterAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public ServiceFilterAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            ServiceType = type;
        }

        /// <inheritdoc />
        public int Order { get; set; }

        public Type ServiceType { get; private set; }

        /// <inheritdoc />
        public bool IsReusable { get; set; }

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var service = serviceProvider.GetRequiredService(ServiceType);

            var filter = service as IFilterMetadata;
            if (filter == null)
            {
                throw new InvalidOperationException(Resources.FormatFilterFactoryAttribute_TypeMustImplementIFilter(
                    typeof(ServiceFilterAttribute).Name,
                    typeof(IFilterMetadata).Name));
            }

            return filter;
        }
    }
}
