// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Contracts;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class DefaultFilterProvider : INestedProvider<FilterProviderContext>
    {
        public DefaultFilterProvider(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public int Order
        {
            get { return DefaultOrder.DefaultFrameworkSortOrder; }
        }

        protected IServiceProvider ServiceProvider { get; private set; }

        public virtual void Invoke(FilterProviderContext context, Action callNext)
        {
            if (context.ActionContext.ActionDescriptor.FilterDescriptors != null)
            {
                foreach (var item in context.Results)
                {
                    ProvideFilter(context, item);
                }
            }

            var controllerFilter = context.ActionContext.Controller as IFilter;
            if (controllerFilter != null)
            {
                InsertControllerAsFilter(context, controllerFilter);
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

        private void InsertControllerAsFilter(FilterProviderContext context, IFilter controllerFilter)
        {
            var descriptor = new FilterDescriptor(controllerFilter, FilterScope.Controller);
            var item = new FilterItem(descriptor, controllerFilter);

            // BinarySearch will return the index of where the item _should_be_ in the list.
            //
            // If index > 0: 
            //      Other items in the list have the same order and scope - the item was 'found'.
            //
            // If index < 0: 
            //      No other items in the list have the same order and scope - the item was not 'found'
            //      Index will be the bitwise compliment of of the 'right' location.
            var index = context.Results.BinarySearch(item, FilterItemOrderComparer.Comparer);
            if (index < 0)
            {
                index = ~index;
            }

            context.Results.Insert(index, item);
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
