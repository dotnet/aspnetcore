using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class ReflectedActionDescriptorProvider : IActionDescriptorProvider
    {
        private readonly IControllerAssemblyProvider _controllerAssemblyProvider;
        private readonly IActionDiscoveryConventions _conventions;
        private readonly IControllerDescriptorFactory _controllerDescriptorFactory;
        private readonly IParameterDescriptorFactory _parameterDescriptorFactory;

        public ReflectedActionDescriptorProvider(IControllerAssemblyProvider controllerAssemblyProvider,
                                                 IActionDiscoveryConventions conventions,
                                                 IControllerDescriptorFactory controllerDescriptorFactory,
                                                 IParameterDescriptorFactory parameterDescriptorFactory)
        {
            _controllerAssemblyProvider = controllerAssemblyProvider;
            _conventions = conventions;
            _controllerDescriptorFactory = controllerDescriptorFactory;
            _parameterDescriptorFactory = parameterDescriptorFactory;
        }

        public int Order
        {
            get { return 0; }
        }

        public void Invoke(ActionDescriptorProviderContext context, Action callNext)
        {
            context.Results.AddRange(GetDescriptors());
            callNext();
        }

        public IEnumerable<ActionDescriptor> GetDescriptors()
        {
            var assemblies = _controllerAssemblyProvider.Assemblies;
            var types = assemblies.SelectMany(a => a.DefinedTypes);
            var controllers = types.Where(_conventions.IsController);
            var controllerDescriptors = controllers.Select(t => _controllerDescriptorFactory.CreateControllerDescriptor(t)).ToArray();

            foreach (var cd in controllerDescriptors)
            {
                var controllerFilters = GetOrderedFilterAttributes(cd.ControllerTypeInfo);


                bool allowAnonymous = IsAnonymous(controllerAttributes);

                foreach (var methodInfo in cd.ControllerTypeInfo.DeclaredMethods)
                {
                    var actionInfos = _conventions.GetActions(methodInfo);

                    if (actionInfos == null)
                    {
                        continue;
                    }

                    foreach (var actionInfo in actionInfos)
                    {
                        yield return BuildDescriptor(cd, methodInfo, actionInfo, controllerFilters);
                    }
                }
            }
        }

        private IFilter[] GetOrderedFilterAttributes(MemberInfo memberInfo)
        {
            var attributes = memberInfo.GetCustomAttributes(inherit: true);
            var filters = attributes.OfType<IFilter>().OrderByDescending(filter => filter.Order);

            return filters.ToArray();
        }

        private ReflectedActionDescriptor BuildDescriptor(ControllerDescriptor controllerDescriptor,
                                                          MethodInfo methodInfo,
                                                          ActionInfo actionInfo,
                                                          IFilter[] controllerFilters)
        private ReflectedActionDescriptor BuildDescriptor(ControllerDescriptor controllerDescriptor,
                                                          MethodInfo methodInfo,
                                                          ActionInfo actionInfo,
                                                          IFilter[] controllerFilters,
                                                          bool allowAnonymous)
        {
            var ad = new ReflectedActionDescriptor
            {
                RouteConstraints = new List<RouteDataActionConstraint>
                {
                    new RouteDataActionConstraint("controller", controllerDescriptor.Name)
                },

                Name = actionInfo.ActionName,
                ControllerDescriptor = controllerDescriptor,
                MethodInfo = methodInfo,
            };

            var httpMethods = actionInfo.HttpMethods;
            if (httpMethods != null && httpMethods.Length > 0)
            {
                ad.MethodConstraints = new List<HttpMethodConstraint>
                {
                    new HttpMethodConstraint(httpMethods)
                };
            }

            if (actionInfo.RequireActionNameMatch)
            {
                ad.RouteConstraints.Add(new RouteDataActionConstraint("action", actionInfo.ActionName));
            }
            else
            {
                ad.RouteConstraints.Add(new RouteDataActionConstraint("action", RouteKeyHandling.DenyKey));
            }

            ad.Parameters = methodInfo.GetParameters().Select(p => _parameterDescriptorFactory.GetDescriptor(p)).ToList();

            // TODO: add ordering support such that action filters are ahead of controller filters if they have the same order
            var actionFilters = GetOrderedFilterAttributes(methodInfo);

            ad.Filters = MergeSorted(actionFilters, controllerFilters);

            ad.Filters = MergeSorted(filtersFromAction, controllerFilters);

            ad.AllowAnonymous = allowAnonymous || IsAnonymous(attributes);

            return ad;
        }

        internal List<IFilter> MergeSorted(IFilter[] actionFilters, IFilter[] controllerFilters)
        {
            var list = new List<IFilter>();

            var count = actionFilters.Length + controllerFilters.Length;

            for (int i = 0, j = 0; i + j < count; )
            {
                if (i >= actionFilters.Length)
                {
                    list.Add(controllerFilters[j++]);
                }
                else if (j >= controllerFilters.Length)
                {
                    list.Add(actionFilters[i++]);
                }
                else if (actionFilters[i].Order >= controllerFilters[j].Order)
                {
                    list.Add(actionFilters[i++]);
                }
                else
                {
                    list.Add(controllerFilters[j++]);
                }
            }

            return list;
        }
    }
}
