// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    [DebuggerDisplay("ServiceFilter: Type={ServiceType} Order={Order}")]
    public class ServiceFilterAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public ServiceFilterAttribute([NotNull] Type type)
        {
            ServiceType = type;
        }

        public Type ServiceType { get; private set; }

        public int Order { get; set; }

        public IFilter CreateInstance([NotNull] IServiceProvider serviceProvider)
        {
            var service = serviceProvider.GetRequiredService(ServiceType);

            var filter = service as IFilter;
            if (filter == null)
            {
                throw new InvalidOperationException(Resources.FormatFilterFactoryAttribute_TypeMustImplementIFilter(
                    typeof(ServiceFilterAttribute).Name,
                    typeof(IFilter).Name));
            }

            return filter;
        }
    }
}
