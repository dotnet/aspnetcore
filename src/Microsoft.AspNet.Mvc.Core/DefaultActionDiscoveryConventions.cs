using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultActionDiscoveryConventions : IActionDiscoveryConventions
    {
        private static readonly string[] _supportedHttpMethodsByConvention =
        {
            "GET",
            "POST",
            "PUT",
            "DELETE",
            "PATCH",
        };

        private static readonly string[] _supportedHttpMethodsForDefaultMethod =
        {
            "GET",
            "POST"
        };

        public virtual string DefaultMethodName
        {
            get { return "Index"; }
        }

        public virtual bool IsController([NotNull] TypeInfo typeInfo)
        {
            if (!typeInfo.IsClass ||
                typeInfo.IsAbstract ||
                typeInfo.ContainsGenericParameters)
            {
                return false;
            }

            if (typeInfo.Name.Equals("Controller", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) ||
                   typeof(Controller).GetTypeInfo().IsAssignableFrom(typeInfo);
        }

        // If the convention is All methods starting with Get do not have an action name,
        // for a input GetXYZ methodInfo, the return value will be
        // { { HttpMethods = "GET", ActionName = "GetXYZ", RequireActionNameMatch = false }}
        public virtual IEnumerable<ActionInfo> GetActions(
            [NotNull] MethodInfo methodInfo,
            [NotNull] TypeInfo controllerTypeInfo)
        {
            if (!IsValidActionMethod(methodInfo))
            {
                return null;
            }

            var actionInfos = GetActionsForMethodsWithCustomAttributes(methodInfo);
            if (actionInfos.Any())
            {
                return actionInfos;
            }
            else
            {
                actionInfos = GetActionsForMethodsWithoutCustomAttributes(methodInfo, controllerTypeInfo);
            }

            return actionInfos;
        }

        protected virtual bool IsDefaultActionMethod([NotNull] MethodInfo methodInfo)
        {
            return String.Equals(methodInfo.Name, DefaultMethodName, StringComparison.OrdinalIgnoreCase);
        }

        protected virtual bool IsValidActionMethod(MethodInfo method)
        {
            return
                method.IsPublic &&
                !method.IsAbstract &&
                !method.IsConstructor &&
                !method.IsGenericMethod &&

                // The SpecialName bit is set to flag members that are treated in a special way by some compilers 
                // (such as property accessors and operator overloading methods).
                !method.IsSpecialName;
        }

        public virtual IEnumerable<string> GetSupportedHttpMethods(MethodInfo methodInfo)
        {
            var supportedHttpMethods =
                _supportedHttpMethodsByConvention.FirstOrDefault(
                    httpMethod => methodInfo.Name.Equals(httpMethod, StringComparison.OrdinalIgnoreCase));
            
            if (supportedHttpMethods != null)
            {
                yield return supportedHttpMethods;
            }
        }

        private bool HasCustomAttributes(MethodInfo methodInfo)
        {
            var actionAttributes = GetActionCustomAttributes(methodInfo);
            return actionAttributes.HttpMethodProviderAttributes.Any();
        }

        private ActionAttributes GetActionCustomAttributes(MethodInfo methodInfo)
        {
            var httpMethodConstraints = methodInfo.GetCustomAttributes().OfType<IActionHttpMethodProvider>();
            return new ActionAttributes()
            {
                HttpMethodProviderAttributes = httpMethodConstraints
            };
        }

        private IEnumerable<ActionInfo> GetActionsForMethodsWithCustomAttributes(MethodInfo methodInfo)
        {
            var httpMethodConstraints = GetActionCustomAttributes(methodInfo).HttpMethodProviderAttributes;
            if (!httpMethodConstraints.Any())
            {
                yield break;
            }

            var httpMethods = httpMethodConstraints.SelectMany(x => x.HttpMethods).Distinct().ToArray();
            if (httpMethods.Any())
            {
                // Any method which does not follow convention and does not have
                // an explicit NoAction attribute is exposed as a method with action name.
                yield return new ActionInfo()
                {
                    HttpMethods = httpMethods,
                    ActionName = methodInfo.Name,
                    RequireActionNameMatch = true
                };
            }
        }

        private IEnumerable<ActionInfo> GetActionsForMethodsWithoutCustomAttributes(MethodInfo methodInfo, TypeInfo controllerTypeInfo)
        {
            var actionInfos = new List<ActionInfo>();
            var httpMethods = GetSupportedHttpMethods(methodInfo);
            if (httpMethods != null && httpMethods.Any())
            {
                return new[]
                {
                    new ActionInfo()
                    {
                        HttpMethods = httpMethods.ToArray(),
                        ActionName = methodInfo.Name,
                        RequireActionNameMatch = false,
                    }
                };
            }

            // For Default Method add an action Info with GET, POST Http Method constraints.
            // Only constraints (out of GET and POST) for which there are no convention based actions available are added.
            // If there are existing action infos with http constraints for GET and POST, this action info is not added for default method. 
            if (IsDefaultActionMethod(methodInfo))
            {
                var existingHttpMethods = new HashSet<string>();
                foreach (var declaredMethodInfo in controllerTypeInfo.DeclaredMethods)
                {
                    if (!IsValidActionMethod(declaredMethodInfo) || HasCustomAttributes(declaredMethodInfo))
                    {
                        continue;
                    }

                    httpMethods = GetSupportedHttpMethods(declaredMethodInfo);
                    if (httpMethods != null)
                    {
                        existingHttpMethods.UnionWith(httpMethods);
                    }
                }
                var undefinedHttpMethods = _supportedHttpMethodsForDefaultMethod.Except(
                                                                                     existingHttpMethods,
                                                                                     StringComparer.Ordinal)
                                                                                .ToArray();
                if (undefinedHttpMethods.Any())
                {
                    actionInfos.Add(new ActionInfo()
                    {
                        HttpMethods = undefinedHttpMethods,
                        ActionName = methodInfo.Name,
                        RequireActionNameMatch = false,
                    });
                }
            }

            actionInfos.Add(
                new ActionInfo()
                {
                    ActionName = methodInfo.Name,
                    RequireActionNameMatch = true,
                });

            return actionInfos;
        }

        private class ActionAttributes
        {
            public IEnumerable<IActionHttpMethodProvider> HttpMethodProviderAttributes { get; set; } 
        }
    }
}