// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

                // We only consider public top-level classes as controllers. IsPublic returns false for nested
                // classes, regardless of visibility modifiers.
                !typeInfo.IsPublic ||
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

        /// <summary>
        /// Determines whether the method is a valid action.
        /// </summary>
        /// <param name="method">The <see cref="MethodInfo"/>.</param>
        /// <returns>true if the method is a valid action. Otherwise, false.</returns>
        public virtual bool IsValidActionMethod(MethodInfo method)
        {
            return
                method.IsPublic &&
                !method.IsStatic &&
                !method.IsAbstract &&
                !method.IsConstructor &&
                !method.IsGenericMethod &&

                // The SpecialName bit is set to flag members that are treated in a special way by some compilers
                // (such as property accessors and operator overloading methods).
                !method.IsSpecialName &&
                !method.IsDefined(typeof(NonActionAttribute)) &&

                // Overriden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
                method.GetBaseDefinition().DeclaringType != typeof(object);
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
            return actionAttributes.Any();
        }

        private ActionAttributes GetActionCustomAttributes(MethodInfo methodInfo)
        {
            var attributes = methodInfo.GetCustomAttributes();
            var actionNameAttribute = attributes.OfType<ActionNameAttribute>().FirstOrDefault();
            var httpMethodConstraints = attributes.OfType<IActionHttpMethodProvider>();
            return new ActionAttributes()
            {
                HttpMethodProviderAttributes = httpMethodConstraints,
                ActionNameAttribute = actionNameAttribute
            };
        }

        private IEnumerable<ActionInfo> GetActionsForMethodsWithCustomAttributes(MethodInfo methodInfo)
        {
            var actionAttributes = GetActionCustomAttributes(methodInfo);
            if (!actionAttributes.Any())
            {
                // If the action is not decorated with any of the attributes,
                // it would be handled by convention.
                yield break;
            }

            var actionNameAttribute = actionAttributes.ActionNameAttribute;
            var actionName = actionNameAttribute != null ? actionNameAttribute.Name : methodInfo.Name;

            var httpMethodProviders = actionAttributes.HttpMethodProviderAttributes;
            var httpMethods = httpMethodProviders.SelectMany(x => x.HttpMethods).Distinct().ToArray();

            yield return new ActionInfo()
            {
                HttpMethods = httpMethods,
                ActionName = actionName,
                RequireActionNameMatch = true
            };
        }

        private IEnumerable<ActionInfo> GetActionsForMethodsWithoutCustomAttributes(
            MethodInfo methodInfo,
            TypeInfo controllerTypeInfo)
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
            // Only constraints (out of GET and POST) for which there are no convention based actions available are
            // added. If there are existing action infos with http constraints for GET and POST, this action info is
            // not added for default method.
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
            public ActionNameAttribute ActionNameAttribute { get; set; }

            public bool Any()
            {
                return ActionNameAttribute != null || HttpMethodProviderAttributes.Any();
            }
        }
    }
}