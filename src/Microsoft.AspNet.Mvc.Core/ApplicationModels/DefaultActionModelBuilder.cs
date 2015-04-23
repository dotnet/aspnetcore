// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Cors;
using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc.ApiExplorer;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    /// <summary>
    /// A default implementation of <see cref="IActionModelBuilder"/>.
    /// </summary>
    public class DefaultActionModelBuilder : IActionModelBuilder
    {
        private readonly AuthorizationOptions _authorizationOptions;

        public DefaultActionModelBuilder(IOptions<AuthorizationOptions> authorizationOptions)
        {
            _authorizationOptions = authorizationOptions?.Options ?? new AuthorizationOptions();
        }

        /// <inheritdoc />
        public IEnumerable<ActionModel> BuildActionModels([NotNull] TypeInfo typeInfo, [NotNull] MethodInfo methodInfo)
        {
            if (!IsAction(typeInfo, methodInfo))
            {
                return Enumerable.Empty<ActionModel>();
            }

            // CoreCLR returns IEnumerable<Attribute> from GetCustomAttributes - the OfType<object>
            // is needed to so that the result of ToArray() is object
            var attributes = methodInfo.GetCustomAttributes(inherit: true).OfType<object>().ToArray();

            // Route attributes create multiple actions, we want to split the set of
            // attributes based on these so each action only has the attributes that affect it.
            //
            // The set of route attributes are split into those that 'define' a route versus those that are
            // 'silent'.
            //
            // We need to define an action for each attribute that 'defines' a route, and a single action
            // for all of the ones that don't (if any exist).
            //
            // If the attribute that 'defines' a route is NOT an IActionHttpMethodProvider, then we'll include with
            // it, any IActionHttpMethodProvider that are 'silent' IRouteTemplateProviders. In this case the 'extra'
            // action for silent route providers isn't needed.
            //
            // Ex:
            // [HttpGet]
            // [AcceptVerbs("POST", "PUT")]
            // [HttpPost("Api/Things")]
            // public void DoThing()
            //
            // This will generate 2 actions:
            // 1. [HttpPost("Api/Things")]
            // 2. [HttpGet], [AcceptVerbs("POST", "PUT")]
            //
            // Note that having a route attribute that doesn't define a route template _might_ be an error. We
            // don't have enough context to really know at this point so we just pass it on.
            var routeProviders = new List<object>();

            var createActionForSilentRouteProviders = false;
            foreach (var attribute in attributes)
            {
                var routeTemplateProvider = attribute as IRouteTemplateProvider;
                if (routeTemplateProvider != null)
                {
                    if (IsSilentRouteAttribute(routeTemplateProvider))
                    {
                        createActionForSilentRouteProviders = true;
                    }
                    else
                    {
                        routeProviders.Add(attribute);
                    }
                }
            }

            foreach (var routeProvider in routeProviders)
            {
                // If we see an attribute like
                // [Route(...)]
                //
                // Then we want to group any attributes like [HttpGet] with it.
                //
                // Basically...
                //
                // [HttpGet]
                // [HttpPost("Products")]
                // public void Foo() { }
                //
                // Is two actions. And...
                //
                // [HttpGet]
                // [Route("Products")]
                // public void Foo() { }
                //
                // Is one action.
                if (!(routeProvider is IActionHttpMethodProvider))
                {
                    createActionForSilentRouteProviders = false;
                }
            }

            var actionModels = new List<ActionModel>();
            if (routeProviders.Count == 0 && !createActionForSilentRouteProviders)
            {
                actionModels.Add(CreateActionModel(methodInfo, attributes));
            }
            else
            {
                // Each of these routeProviders are the ones that actually have routing information on them
                // something like [HttpGet] won't show up here, but [HttpGet("Products")] will.
                foreach (var routeProvider in routeProviders)
                {
                    var filteredAttributes = new List<object>();
                    foreach (var attribute in attributes)
                    {
                        if (attribute == routeProvider)
                        {
                            filteredAttributes.Add(attribute);
                        }
                        else if (routeProviders.Contains(attribute))
                        {
                            // Exclude other route template providers
                        }
                        else if (
                            routeProvider is IActionHttpMethodProvider &&
                            attribute is IActionHttpMethodProvider)
                        {
                            // Exclude other http method providers if this route is an
                            // http method provider.
                        }
                        else
                        {
                            filteredAttributes.Add(attribute);
                        }
                    }

                    actionModels.Add(CreateActionModel(methodInfo, filteredAttributes));
                }

                if (createActionForSilentRouteProviders)
                {
                    var filteredAttributes = new List<object>();
                    foreach (var attribute in attributes)
                    {
                        if (!routeProviders.Contains(attribute))
                        {
                            filteredAttributes.Add(attribute);
                        }
                    }

                    actionModels.Add(CreateActionModel(methodInfo, filteredAttributes));
                }
            }

            foreach (var actionModel in actionModels)
            {
                foreach (var parameterInfo in actionModel.ActionMethod.GetParameters())
                {
                    var parameterModel = CreateParameterModel(parameterInfo);
                    if (parameterModel != null)
                    {
                        parameterModel.Action = actionModel;
                        actionModel.Parameters.Add(parameterModel);
                    }
                }
            }

            return actionModels;
        }

        /// <summary>
        /// Returns <c>true</c> if the <paramref name="methodInfo"/> is an action. Otherwise <c>false</c>.
        /// </summary>
        /// <param name="methodInfo">The <see cref="MethodInfo"/>.</param>
        /// <param name="typeInfo">The <see cref="TypeInfo"/>.</param>
        /// <returns><c>true</c> if the <paramref name="methodInfo"/> is an action. Otherwise <c>false</c>.</returns>
        /// <remarks>
        /// Override this method to provide custom logic to determine which methods are considered actions.
        /// </remarks>
        protected virtual bool IsAction([NotNull] TypeInfo typeInfo, [NotNull] MethodInfo methodInfo)
        {
            // The SpecialName bit is set to flag members that are treated in a special way by some compilers
            // (such as property accessors and operator overloading methods).
            if (methodInfo.IsSpecialName)
            {
                return false;
            }

            if (methodInfo.IsDefined(typeof(NonActionAttribute)))
            {
                return false;
            }

            // Overriden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
            if (methodInfo.GetBaseDefinition().DeclaringType == typeof(object))
            {
                return false;
            }

            // Dispose method implemented from IDisposable is not valid
            if (IsIDisposableMethod(methodInfo, typeInfo))
            {
                return false;
            }

            if (methodInfo.IsStatic)
            {
                return false;
            }

            if (methodInfo.IsAbstract)
            {
                return false;
            }

            if (methodInfo.IsConstructor)
            {
                return false;
            }

            if (methodInfo.IsGenericMethod)
            {
                return false;
            }

            return
                methodInfo.IsPublic;
        }

        private bool IsIDisposableMethod(MethodInfo methodInfo, TypeInfo typeInfo)
        {
            return
                (typeof(IDisposable).GetTypeInfo().IsAssignableFrom(typeInfo) &&
                 typeInfo.GetRuntimeInterfaceMap(typeof(IDisposable)).TargetMethods[0] == methodInfo);
        }

        /// <summary>
        /// Creates an <see cref="ActionModel"/> for the given <see cref="MethodInfo"/>.
        /// </summary>
        /// <param name="methodInfo">The <see cref="MethodInfo"/>.</param>
        /// <param name="attributes">The set of attributes to use as metadata.</param>
        /// <returns>An <see cref="ActionModel"/> for the given <see cref="MethodInfo"/>.</returns>
        /// <remarks>
        /// An action-method in code may expand into multiple <see cref="ActionModel"/> instances depending on how
        /// the action is routed. In the case of multiple routing attributes, this method will invoked be once for
        /// each action that can be created.
        ///
        /// If overriding this method, use the provided <paramref name="attributes"/> list to find metadata related to
        /// the action being created.
        /// </remarks>
        protected virtual ActionModel CreateActionModel(
            [NotNull] MethodInfo methodInfo,
            [NotNull] IReadOnlyList<object> attributes)
        {
            var actionModel = new ActionModel(methodInfo, attributes);

            AddRange(actionModel.ActionConstraints, attributes.OfType<IActionConstraintMetadata>());
            AddRange(actionModel.Filters, attributes.OfType<IFilter>());

            var enableCors = attributes.OfType<IEnableCorsAttribute>().SingleOrDefault();
            if (enableCors != null)
            {
                actionModel.Filters.Add(new CorsAuthorizationFilterFactory(enableCors.PolicyName));
            }

            var disableCors = attributes.OfType<IDisableCorsAttribute>().SingleOrDefault();
            if (disableCors != null)
            {
                actionModel.Filters.Add(new DisableCorsAuthorizationFilter());
            }

            var policy = AuthorizationPolicy.Combine(_authorizationOptions, attributes.OfType<AuthorizeAttribute>());
            if (policy != null)
            {
                actionModel.Filters.Add(new AuthorizeFilter(policy));
            }

            var actionName = attributes.OfType<ActionNameAttribute>().FirstOrDefault();
            if (actionName?.Name != null)
            {
                actionModel.ActionName = actionName.Name;
            }
            else
            {
                actionModel.ActionName = methodInfo.Name;
            }

            var apiVisibility = attributes.OfType<IApiDescriptionVisibilityProvider>().FirstOrDefault();
            if (apiVisibility != null)
            {
                actionModel.ApiExplorer.IsVisible = !apiVisibility.IgnoreApi;
            }

            var apiGroupName = attributes.OfType<IApiDescriptionGroupNameProvider>().FirstOrDefault();
            if (apiGroupName != null)
            {
                actionModel.ApiExplorer.GroupName = apiGroupName.GroupName;
            }

            var httpMethods = attributes.OfType<IActionHttpMethodProvider>();
            AddRange(actionModel.HttpMethods,
                httpMethods
                    .Where(a => a.HttpMethods != null)
                    .SelectMany(a => a.HttpMethods)
                    .Distinct());

            AddRange(actionModel.RouteConstraints, attributes.OfType<IRouteConstraintProvider>());

            var routeTemplateProvider =
                attributes
                .OfType<IRouteTemplateProvider>()
                .Where(a => !IsSilentRouteAttribute(a))
                .SingleOrDefault();

            if (routeTemplateProvider != null)
            {
                actionModel.AttributeRouteModel = new AttributeRouteModel(routeTemplateProvider);
            }

            return actionModel;
        }

        /// <summary>
        /// Creates a <see cref="ParameterModel"/> for the given <see cref="ParameterInfo"/>.
        /// </summary>
        /// <param name="parameterInfo">The <see cref="ParameterInfo"/>.</param>
        /// <returns>A <see cref="ParameterModel"/> for the given <see cref="ParameterInfo"/>.</returns>
        protected virtual ParameterModel CreateParameterModel([NotNull] ParameterInfo parameterInfo)
        {
            // CoreCLR returns IEnumerable<Attribute> from GetCustomAttributes - the OfType<object>
            // is needed to so that the result of ToArray() is object
            var attributes = parameterInfo.GetCustomAttributes(inherit: true).OfType<object>().ToArray();
            var parameterModel = new ParameterModel(parameterInfo, attributes);

            var bindingInfo = BindingInfo.GetBindingInfo(attributes);
            parameterModel.BindingInfo = bindingInfo;

            parameterModel.ParameterName = parameterInfo.Name;

            return parameterModel;
        }

        private bool IsSilentRouteAttribute(IRouteTemplateProvider routeTemplateProvider)
        {
            return
                routeTemplateProvider.Template == null &&
                routeTemplateProvider.Order == null &&
                routeTemplateProvider.Name == null;
        }

        private static void AddRange<T>(IList<T> list, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }
    }
}