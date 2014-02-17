using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class TypeMethodBasedActionDescriptorProvider : IActionDescriptorProvider
    {
        private readonly IControllerAssemblyProvider _controllerAssemblyProvider;
        private readonly IActionDiscoveryConventions _conventions;
        private readonly IControllerDescriptorFactory _controllerDescriptorFactory;

        public TypeMethodBasedActionDescriptorProvider(IControllerAssemblyProvider controllerAssemblyProvider,
                                                       IActionDiscoveryConventions conventions,
                                                       IControllerDescriptorFactory controllerDescriptorFactory)
        {
            _controllerAssemblyProvider = controllerAssemblyProvider;
            _conventions = conventions;
            _controllerDescriptorFactory = controllerDescriptorFactory;
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

        private static TypeMethodBasedActionDescriptor BuildDescriptor(ControllerDescriptor controllerDescriptor, MethodInfo methodInfo, ActionInfo actionInfo)
        {
            var ad = new TypeMethodBasedActionDescriptor
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

            return ad;
        }

        private static void ApplyRest(TypeMethodBasedActionDescriptor descriptor, IEnumerable<string> httpMethods)
        {

            descriptor.RouteConstraints.Add(new RouteDataActionConstraint("action", RouteKeyHandling.DenyKey));
        }

        private static void ApplyRpc(TypeMethodBasedActionDescriptor descriptor, ActionInfo convention)
        {

            var methods = convention.HttpMethods;

            // rest action require specific methods, but RPC actions do not.
            if (methods != null)
            {
                descriptor.MethodConstraints = new List<HttpMethodConstraint>
                {
                    new HttpMethodConstraint(methods)
                };
            }
        }
    }
}
