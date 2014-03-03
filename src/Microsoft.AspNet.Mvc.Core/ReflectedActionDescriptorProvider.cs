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
                foreach (var methodInfo in cd.ControllerTypeInfo.DeclaredMethods)
                {
                    var actionInfos = _conventions.GetActions(methodInfo);

                    if (actionInfos == null)
                    {
                        continue;
                    }

                    foreach (var actionInfo in actionInfos)
                    {
                        yield return BuildDescriptor(cd, methodInfo, actionInfo);
                    }
                }
            }
        }

        private ReflectedActionDescriptor BuildDescriptor(ControllerDescriptor controllerDescriptor, MethodInfo methodInfo, ActionInfo actionInfo)
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

            return ad;
        }
    }
}
