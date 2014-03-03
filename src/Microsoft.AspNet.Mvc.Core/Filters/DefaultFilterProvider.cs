using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultFilterProvider : INestedProvider<FilterProviderContext>
    {
        public DefaultFilterProvider(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }


        public virtual void Invoke(FilterProviderContext context, Action callNext)
        {
            List<IFilter> filters = context.ActionDescriptor.Filters;

            if (filters == null)
            {
                filters = new List<IFilter>();
            }
            else
            {
                filters = filters.ToList(); // make a copy of the list, TODO: Make the actiondescriptor immutable

            }

            AddGlobalFilters(filters);

            if (filters.Count > 0)
            {
                for (int i = 0; i < filters.Count; i++)
                {
                    GetFilter(context, filters[i]);
                }
            }

            if (callNext != null)
            {
                callNext();
            }
        }

        public virtual void GetFilter(FilterProviderContext context, IFilter filter)
        {
            var serviceFilterSignature = filter as IServiceFilter;
            if (serviceFilterSignature != null)
            {
                var serviceFilter = ServiceProvider.GetService(serviceFilterSignature.ServiceType);

                AddFilters(context, serviceFilter, true);

                // if the filter implements more than the just IServiceFilter
                AddFilters(context, filter, false);
            }
            else
            {
                AddFilters(context, filter, true);
            }
        }

        public virtual List<IFilter> AddGlobalFilters(List<IFilter> filters)
        {
            var globalFilters = ServiceProvider.GetService<IEnumerable<IFilter>>().AsArray();

            if (globalFilters == null || globalFilters.Length == 0)
            {
                return filters;
            }

            return MergeSorted(filters, globalFilters);
        }

        private List<IFilter> MergeSorted(List<IFilter> filtersFromAction, IFilter[] globalFilters) 
        {
            if (globalFilters.Length == 0)
            {
                return filtersFromAction;
            }

            var list = new List<IFilter>();

            var count = filtersFromAction.Count + globalFilters.Length;

            for (int i = 0, j = 0; i + j < count; )
            {
                if (i >= filtersFromAction.Count)
                {
                    list.Add(globalFilters[j++]);
                }
                else if (j >= globalFilters.Length)
                {
                    list.Add(filtersFromAction[i++]);
                }
                else if (filtersFromAction[i].Order >= globalFilters[j].Order)
                {
                    list.Add(filtersFromAction[i++]);
                }
                else
                {
                    list.Add(globalFilters[j++]);
                }
            }

            return list;
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
                throw  new InvalidOperationException("Filter has to be IActionResultFilter, IActionFilter, IExceptionFilter or IAuthorizationFilter.");
            }
        }
    }
}
