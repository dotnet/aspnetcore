// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultActionDiscoveryConventions : IActionDiscoveryConventions
    {
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
        // { { HttpMethods = "GET", ActionName = "GetXYZ", RequireActionNameMatch = false, AttributeRoute = null }}
        public virtual IEnumerable<ActionInfo> GetActions(
            [NotNull] MethodInfo methodInfo,
            [NotNull] TypeInfo controllerTypeInfo)
        {
            if (!IsValidActionMethod(methodInfo))
            {
                return null;
            }

            var attributes = GetActionCustomAttributes(methodInfo);
            var actionInfos = GetActionsForMethodsWithCustomAttributes(attributes, methodInfo, controllerTypeInfo);
            if (actionInfos.Any())
            {
                return actionInfos;
            }
            else
            {
                // By default the action is just matched by name.
                actionInfos = new ActionInfo[]
                {
                    new ActionInfo()
                    {
                        ActionName = methodInfo.Name,
                        Attributes = attributes.Attributes,
                        RequireActionNameMatch = true,
                    }
                };
            }

            return actionInfos;
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

        private ActionAttributes GetActionCustomAttributes(MethodInfo methodInfo)
        {
            var attributes = methodInfo.GetCustomAttributes(inherit: true).OfType<object>().ToArray();
            var actionNameAttribute = attributes.OfType<ActionNameAttribute>().FirstOrDefault();
            var httpMethodConstraints = attributes.OfType<IActionHttpMethodProvider>();
            var routeTemplates = attributes.OfType<IRouteTemplateProvider>();

            return new ActionAttributes()
            {
                Attributes = attributes,
                ActionNameAttribute = actionNameAttribute,
                HttpMethodProviderAttributes = httpMethodConstraints,
                RouteTemplateProviderAttributes = routeTemplates,
            };
        }

        private IEnumerable<ActionInfo> GetActionsForMethodsWithCustomAttributes(
            ActionAttributes actionAttributes,
            MethodInfo methodInfo,
            TypeInfo controller)
        {
            var hasControllerAttributeRoutes = HasValidControllerRouteTemplates(controller);

            // We need to check controllerRouteTemplates to take into account the
            // case where the controller has [Route] on it and the action does not have any
            // attributes applied to it.
            if (actionAttributes.HasSpecialAttribute() || hasControllerAttributeRoutes)
            {
                var actionNameAttribute = actionAttributes.ActionNameAttribute;
                var actionName = actionNameAttribute != null ? actionNameAttribute.Name : methodInfo.Name;

                // The moment we see a non null attribute route template in the method or
                // in the controller we consider the whole group to be attribute routed actions.
                // If a combination ends up producing a non attribute routed action we consider
                // that an error and throw at a later point in the pipeline.
                if (hasControllerAttributeRoutes || ActionHasAttributeRoutes(actionAttributes))
                {
                    return GetAttributeRoutedActions(actionAttributes, actionName);
                }
                else
                {
                    return GetHttpConstrainedActions(actionAttributes, actionName);
                }
            }
            else
            {
                // If the action is not decorated with any of the attributes,
                // it would be handled by convention.
                return Enumerable.Empty<ActionInfo>();
            }
        }

        private static bool ActionHasAttributeRoutes(ActionAttributes actionAttributes)
        {
            // We neet to check for null as some attributes implement IActionHttpMethodProvider
            // and IRouteTemplateProvider and allow the user to provide a null template. An example
            // of this is HttpGetAttribute. If the user provides a template, the attribute marks the
            // action as attribute routed, but in other case, the attribute only adds a constraint
            // that allows the action to be called with the GET HTTP method.
            return actionAttributes.RouteTemplateProviderAttributes
                .Any(rtp => rtp.Template != null);
        }

        private static bool HasValidControllerRouteTemplates(TypeInfo controller)
        {
            // A method inside a controller is considered to create attribute routed actions if the controller
            // has one or more attributes that implement IRouteTemplateProvider with a non null template applied
            // to it.
            return controller.GetCustomAttributes()
                            .OfType<IRouteTemplateProvider>()
                            .Any(cr => cr.Template != null);
        }

        private static IEnumerable<ActionInfo> GetHttpConstrainedActions(
            ActionAttributes actionAttributes,
            string actionName)
        {
            var httpMethodProviders = actionAttributes.HttpMethodProviderAttributes;
            var httpMethods = httpMethodProviders.SelectMany(x => x.HttpMethods).Distinct().ToArray();
            if (httpMethods.Length > 0)
            {
                foreach (var httpMethod in httpMethods)
                {
                    yield return new ActionInfo()
                    {
                        HttpMethods = new string[] { httpMethod },
                        ActionName = actionName,
                        Attributes = actionAttributes.Attributes,
                        RequireActionNameMatch = true,
                    };
                }
            }
            else
            {
                yield return new ActionInfo()
                {
                    HttpMethods = httpMethods,
                    ActionName = actionName,
                    Attributes = actionAttributes.Attributes,
                    RequireActionNameMatch = true,
                };
            }
        }

        private static IEnumerable<ActionInfo> GetAttributeRoutedActions(
            ActionAttributes actionAttributes,
            string actionName)
        {
            var actions = new List<ActionInfo>();

            // This is the case where the controller has [Route] applied to it and
            // the action doesn't have any [Route] or [Http*] attribute applied.
            if (!actionAttributes.RouteTemplateProviderAttributes.Any())
            {
                actions.Add(new ActionInfo
                {
                    Attributes = actionAttributes.Attributes,
                    ActionName = actionName,
                    HttpMethods = null,
                    RequireActionNameMatch = true,
                    AttributeRoute = null
                });
            }

            foreach (var routeTemplateProvider in actionAttributes.RouteTemplateProviderAttributes)
            {
                // We want to exclude the attributes from the other route template providers;
                var attributes = actionAttributes.Attributes
                    .Where(a => a == routeTemplateProvider || !(a is IRouteTemplateProvider))
                    .ToArray();

                actions.Add(new ActionInfo()
                {
                    Attributes = attributes,
                    ActionName = actionName,
                    HttpMethods = GetRouteTemplateHttpMethods(routeTemplateProvider),
                    RequireActionNameMatch = true,
                    AttributeRoute = routeTemplateProvider
                });
            }

            return actions;
        }

        private static string[] GetRouteTemplateHttpMethods(IRouteTemplateProvider routeTemplateProvider)
        {
            var provider = routeTemplateProvider as IActionHttpMethodProvider;
            if (provider != null && provider.HttpMethods != null)
            {
                return provider.HttpMethods.ToArray();
            }

            return null;
        }

        private class ActionAttributes
        {
            public ActionNameAttribute ActionNameAttribute { get; set; }

            public object[] Attributes { get; set; }

            public IEnumerable<IActionHttpMethodProvider> HttpMethodProviderAttributes { get; set; }
            public IEnumerable<IRouteTemplateProvider> RouteTemplateProviderAttributes { get; set; }

            public bool HasSpecialAttribute()
            {
                return ActionNameAttribute != null ||
                    HttpMethodProviderAttributes.Any() ||
                    RouteTemplateProviderAttributes.Any();
            }
        }
    }
}