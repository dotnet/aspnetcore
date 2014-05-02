// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Diagnostics.Contracts;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class DefaultFilterProvider : INestedProvider<FilterProviderContext>
    {
        private readonly ITypeActivator _typeActivator;

        public DefaultFilterProvider(IServiceProvider serviceProvider, ITypeActivator typeActivator)
        {
            ServiceProvider = serviceProvider;
            _typeActivator = typeActivator;
        }

        public int Order
        {
            get { return 0; }
        }

        protected IServiceProvider ServiceProvider { get; private set; }

        public virtual void Invoke(FilterProviderContext context, Action callNext)
        {
            if (context.ActionDescriptor.FilterDescriptors != null)
            {
                foreach (var item in context.Result)
                {
                    ProvideFilter(context, item);
                }
            }

            if (callNext != null)
            {
                callNext();
            }
        }

        public virtual void ProvideFilter(FilterProviderContext context, FilterItem filterItem)
        {
            if (filterItem.Filter != null)
            {
                return;
            }

            var filter = filterItem.Descriptor.Filter;

            var filterFactory = filter as IFilterFactory;
            if (filterFactory == null)
            {
                filterItem.Filter = filter;
            }
            else
            {
                filterItem.Filter = filterFactory.CreateInstance(ServiceProvider);

                if (filterItem.Filter == null)
                {
                    throw new InvalidOperationException(Resources.FormatTypeMethodMustReturnNotNullValue(
                        "CreateInstance",
                        typeof(IFilterFactory).Name));
                }

                ApplyFilterToContainer(filterItem.Filter, filterFactory);
            }
        }

        private void ApplyFilterToContainer(object actualFilter, IFilter filterMetadata)
        {
            Contract.Assert(actualFilter != null, "actualFilter should not be null");
            Contract.Assert(filterMetadata != null, "filterMetadata should not be null");

            var container = actualFilter as IFilterContainer;

            if (container != null)
            {
                container.FilterDefinition = filterMetadata;
            }
        }
    }
}
