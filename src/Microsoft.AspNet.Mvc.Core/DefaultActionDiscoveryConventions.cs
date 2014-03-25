using System;
using System.Collections.Generic;
using System.Linq;
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
            if (!IsValidMethod(methodInfo))
            {
                return null;
            }

            var actionInfos = new List<ActionInfo>();
            var httpMethods = GetSupportedHttpMethods(methodInfo);
            if (httpMethods != null && httpMethods.Any())
            {
                return new[] {
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
            if (IsDefaultMethod(methodInfo))
            {
                var existingHttpMethods = new HashSet<string>();
                foreach (var validMethodName in controllerTypeInfo.DeclaredMethods)
                {
                    if (!IsValidMethod(validMethodName))
                    {
                        continue;
                    }

                    var methodNames = GetSupportedHttpMethods(validMethodName);
                    if (methodNames != null )
                    {
                        existingHttpMethods.UnionWith(methodNames);
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

        public virtual bool IsDefaultMethod([NotNull] MethodInfo methodInfo)
        {
            return String.Equals(methodInfo.Name, DefaultMethodName, StringComparison.OrdinalIgnoreCase);
        }

        public virtual bool IsValidMethod(MethodInfo method)
        {
            return
                method.IsPublic &&
                !method.IsAbstract &&
                !method.IsConstructor &&
                !method.IsGenericMethod &&
                !method.IsSpecialName;
        }

        public virtual IEnumerable<string> GetSupportedHttpMethods(MethodInfo methodInfo)
        {
            var ret =
                _supportedHttpMethodsByConvention.FirstOrDefault(
                    t => methodInfo.Name.Equals(t, StringComparison.OrdinalIgnoreCase));
            
            if (ret != null)
            {
                yield return ret;
            }
        }
    }
}