using System;
using System.Diagnostics.Contracts;
using Microsoft.AspNet.DependencyInjection;

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

            var serviceFilterSignature = filter as IServiceFilter;
            if (serviceFilterSignature != null)
            {
                var serviceFilter = ServiceProvider.GetService(serviceFilterSignature.ServiceType) as IFilter;

                if (serviceFilter == null)
                {
                    throw new InvalidOperationException("Service filter must be of type IFilter");
                }

                filterItem.Filter = serviceFilter;
            }
            else
            {
                var typeFilterSignature = filter as ITypeFilter;
                if (typeFilterSignature != null)
                {
                    if (typeFilterSignature.ImplementationType == null)
                    {
                        throw new InvalidOperationException("Type filter must provide a type to instantiate");
                    }

                    if (!typeof (IFilter).IsAssignableFrom(typeFilterSignature.ImplementationType))
                    {
                        throw new InvalidOperationException("Type filter must implement IFilter");
                    }

                    // TODO: Move activatorUtilities to come from the service provider.
                    var typeFilter = ActivatorUtilities.CreateInstance(ServiceProvider, typeFilterSignature.ImplementationType) as IFilter;

                    ApplyFilterToContainer(typeFilter, filter);
                    filterItem.Filter = typeFilter;
                }
                else
                {
                    filterItem.Filter = filter;
                }
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
