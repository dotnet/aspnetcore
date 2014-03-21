using System;
using System.Collections;
using System.Collections.Generic;
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

        public virtual bool IsController(TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException("typeInfo");
            }

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

        public IEnumerable<ActionInfo> GetActions(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            if (!IsValidMethod(methodInfo))
            {
                return null;
            }

            for (var i = 0; i < _supportedHttpMethodsByConvention.Length; i++)
            {
                if (methodInfo.Name.Equals(_supportedHttpMethodsByConvention[i], StringComparison.OrdinalIgnoreCase))
                {
                    return new [] {
                        new ActionInfo()
                        {
                            HttpMethods = new[] { _supportedHttpMethodsByConvention[i] },
                            ActionName = methodInfo.Name,
                            RequireActionNameMatch = false,
                        }
                    };
                }
            }

            // TODO: Consider mapping Index here to both Get and also to Index

            return new[]
            {
                new ActionInfo()
                {
                    ActionName = methodInfo.Name,
                    RequireActionNameMatch = true,
                }
            };
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
    }
}
