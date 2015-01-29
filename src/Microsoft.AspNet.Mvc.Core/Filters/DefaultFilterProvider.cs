// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
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
            Debug.Assert(actualFilter != null, "actualFilter should not be null");
            Debug.Assert(filterMetadata != null, "filterMetadata should not be null");

            var container = actualFilter as IFilterContainer;

            if (container != null)
            {
                container.FilterDefinition = filterMetadata;
            }
        }
    }
}
