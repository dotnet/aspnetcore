using System;
using System.Collections.Generic;
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class DefaultFilterProvider : INestedProvider<FilterProviderContext>
    {
        public DefaultFilterProvider(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public virtual void Invoke(FilterProviderContext context, Action callNext)
        {
            FilterDescriptor[] filterDescriptors;

            if (context.ActionDescriptor.FilterDescriptors != null)
            {
                // make a copy of the list, TODO: Make the actiondescriptor immutable
                filterDescriptors = context.ActionDescriptor.FilterDescriptors.ToArray();

                //AddGlobalFilters_moveToAdPipeline(filters);

                if (filterDescriptors.Length > 0)
                {
                    for (int i = 0; i < filterDescriptors.Length; i++)
                    {
                        GetFilter(context, filterDescriptors[i].Filter);
                    }
                }
            }

            if (callNext != null)
            {
                callNext();
            }
        }

        public virtual void GetFilter(FilterProviderContext context, IFilter filter)
        {
            bool failIfNotFilter = true;

            var serviceFilterSignature = filter as IServiceFilter;
            if (serviceFilterSignature != null)
            {
                // TODO: How do we pass extra parameters
                var serviceFilter = ServiceProvider.GetService(serviceFilterSignature.ServiceType);

                AddFilters(context, serviceFilter, true);
                failIfNotFilter = false;
            }

            var typeFilterSignature = filter as ITypeFilter;
            if (typeFilterSignature != null)
            {
                // TODO: How do we pass extra parameters
                var typeFilter = ActivatorUtilities.CreateInstance(ServiceProvider, typeFilterSignature.ImplementationType);

                AddFilters(context, typeFilter, true);
                failIfNotFilter = false;
            }

            AddFilters(context, filter, failIfNotFilter);
        }

        protected IServiceProvider ServiceProvider { get; private set; }

        public int Order
        {
            get { return 0; }
        }

        private void AddFilters(FilterProviderContext context, object filter, bool throwIfNotFilter)
        {
            bool shouldThrow = throwIfNotFilter;

            var authFilter = filter as IAuthorizationFilter;
            var actionFilter = filter as IActionFilter;
            var actionResultFilter = filter as IActionResultFilter;
            var exceptionFilter = filter as IExceptionFilter;

            if (authFilter != null)
            {
                if (context.AuthorizationFilters == null)
                {
                    context.AuthorizationFilters = new List<IAuthorizationFilter>();
                }

                context.AuthorizationFilters.Add(authFilter);
                shouldThrow = false;
            }

            if (actionFilter != null)
            {
                if (context.ActionFilters == null)
                {
                    context.ActionFilters = new List<IActionFilter>();
                }

                context.ActionFilters.Add(actionFilter);
                shouldThrow = false;
            }

            if (actionResultFilter != null)
            {
                if (context.ActionResultFilters == null)
                {
                    context.ActionResultFilters = new List<IActionResultFilter>();
                }

                context.ActionResultFilters.Add(actionResultFilter);
                shouldThrow = false;
            }

            if (exceptionFilter != null)
            {
                if (context.ExceptionFilters == null)
                {
                    context.ExceptionFilters = new List<IExceptionFilter>();
                }

                context.ExceptionFilters.Add(exceptionFilter);
                shouldThrow = false;
            }

            if (shouldThrow)
            {
                throw new InvalidOperationException("Filter has to be IActionResultFilter, IActionFilter, IExceptionFilter or IAuthorizationFilter.");
            }
        }
    }
}
