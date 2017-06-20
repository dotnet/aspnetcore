// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class DefaultPageApplicationModelProvider : IPageApplicationModelProvider
    {
        private const string ModelPropertyName = "Model";
        private readonly FilterCollection _globalFilters;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultPageApplicationModelProvider"/>.
        /// </summary>
        /// <param name="mvcOptions"></param>
        public DefaultPageApplicationModelProvider(IOptions<MvcOptions> mvcOptions)
        {
            _globalFilters = mvcOptions.Value.Filters;
        }

        /// <inheritdoc />
        public int Order => -1000;

        /// <inheritdoc />
        public void OnProvidersExecuting(PageApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.PageApplicationModel = CreateModel(context.ActionDescriptor, context.PageType);
        }

        /// <inheritdoc />
        public void OnProvidersExecuted(PageApplicationModelProviderContext context)
        {
        }

        /// <summary>
        /// Creates a <see cref="PageApplicationModel"/> for the given <paramref name="pageTypeInfo"/>.
        /// </summary>
        /// <param name="actionDescriptor">The <see cref="PageActionDescriptor"/>.</param>
        /// <param name="pageTypeInfo">The <see cref="TypeInfo"/>.</param>
        /// <returns>A <see cref="PageApplicationModel"/> for the given <see cref="TypeInfo"/>.</returns>
        protected virtual PageApplicationModel CreateModel(
            PageActionDescriptor actionDescriptor,
            TypeInfo pageTypeInfo)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            if (pageTypeInfo == null)
            {
                throw new ArgumentNullException(nameof(pageTypeInfo));
            }

            // Pages always have a model type. If it's not set explicitly by the developer using
            // @model, it will be the same as the page type.
            var modelTypeInfo = pageTypeInfo.GetProperty(ModelPropertyName)?.PropertyType?.GetTypeInfo();

            // Now we want to find the handler methods. If the model defines any handlers, then we'll use those,
            // otherwise look at the page itself (unless the page IS the model, in which case we already looked).
            TypeInfo handlerType;

            var handlerModels = modelTypeInfo == null ? null : CreateHandlerModels(modelTypeInfo);
            if (handlerModels?.Count > 0)
            {
                handlerType = modelTypeInfo;
            }
            else
            {
                handlerType = pageTypeInfo.GetTypeInfo();
                handlerModels = CreateHandlerModels(pageTypeInfo);
            }

            var handlerTypeAttributes = handlerType.GetCustomAttributes(inherit: true);
            var pageModel = new PageApplicationModel(
                actionDescriptor,
                handlerType,
                handlerTypeAttributes)
            {
                PageType = pageTypeInfo,
                ModelType = modelTypeInfo,
            };

            for (var i = 0; i < handlerModels.Count; i++)
            {
                var handlerModel = handlerModels[i];
                handlerModel.Page = pageModel;
                pageModel.HandlerMethods.Add(handlerModel);
            }

            PopulateHandlerProperties(pageModel);

            for (var i = 0; i < _globalFilters.Count; i++)
            {
                pageModel.Filters.Add(_globalFilters[i]);
            }

            for (var i = 0; i < handlerTypeAttributes.Length; i++)
            {
                if (handlerTypeAttributes[i] is IFilterMetadata filter)
                {
                    pageModel.Filters.Add(filter);
                }
            }

            return pageModel;
        }

        // Internal for unit testing
        internal void PopulateHandlerProperties(PageApplicationModel pageModel)
        {
            var properties = PropertyHelper.GetVisibleProperties(pageModel.HandlerType.AsType());
            for (var i = 0; i < properties.Length; i++)
            {
                var propertyModel = CreatePropertyModel(properties[i].Property);
                if (propertyModel != null)
                {
                    propertyModel.Page = pageModel;
                    pageModel.HandlerProperties.Add(propertyModel);
                }
            }
        }

        // Internal for unit testing
        internal IList<PageHandlerModel> CreateHandlerModels(TypeInfo handlerTypeInfo)
        {
            var methods = handlerTypeInfo.GetMethods();
            var results = new List<PageHandlerModel>();

            for (var i = 0; i < methods.Length; i++)
            {
                var handler = CreateHandlerModel(methods[i]);
                if (handler != null)
                {
                    results.Add(handler);
                }
            }

            return results;
        }

        /// <summary>
        /// Creates a <see cref="PageHandlerModel"/> for the specified <paramref name="method"/>.s
        /// </summary>
        /// <param name="method">The <see cref="MethodInfo"/>.</param>
        /// <returns>The <see cref="PageHandlerModel"/>.</returns>
        protected virtual PageHandlerModel CreateHandlerModel(MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (!IsHandler(method))
            {
                return null;
            }

            if (method.IsDefined(typeof(NonHandlerAttribute)))
            {
                return null;
            }

            if (method.DeclaringType.GetTypeInfo().IsDefined(typeof(PagesBaseClassAttribute)))
            {
                return null;
            }

            if (!TryParseHandlerMethod(method.Name, out var httpMethod, out var handlerName))
            {
                return null;
            }

            var handlerModel = new PageHandlerModel(
                method,
                method.GetCustomAttributes(inherit: true))
            {
                Name = method.Name,
                HandlerName = handlerName,
                HttpMethod = httpMethod,
            };

            var methodParameters = handlerModel.MethodInfo.GetParameters();

            for (var i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];
                var parameterModel = CreateParameterModel(parameter);
                parameterModel.Handler = handlerModel;

                handlerModel.Parameters.Add(parameterModel);
            }

            return handlerModel;
        }

        /// <summary>
        /// Creates a <see cref="PageParameterModel"/> for the specified <paramref name="parameter"/>.
        /// </summary>
        /// <param name="parameter">The <see cref="ParameterInfo"/>.</param>
        /// <returns>The <see cref="PageParameterModel"/>.</returns>
        protected virtual PageParameterModel CreateParameterModel(ParameterInfo parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            return new PageParameterModel(parameter, parameter.GetCustomAttributes(inherit: true))
            {
                BindingInfo = BindingInfo.GetBindingInfo(parameter.GetCustomAttributes()),
                ParameterName = parameter.Name,
            };
        }

        /// <summary>
        /// Creates a <see cref="PagePropertyModel"/> for the <paramref name="property"/>.
        /// </summary>
        /// <param name="property">The <see cref="PropertyInfo"/>.</param>
        /// <returns>The <see cref="PagePropertyModel"/>.</returns>
        protected virtual PagePropertyModel CreatePropertyModel(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            var attributes = property.GetCustomAttributes(inherit: true);
            var bindingInfo = BindingInfo.GetBindingInfo(attributes);

            var model = new PagePropertyModel(property, property.GetCustomAttributes(inherit: true))
            {
                PropertyName = property.Name,
                BindingInfo = bindingInfo,
            };

            return model;
        }

        /// <summary>
        /// Determines if the specified <paramref name="methodInfo"/> is a handler.
        /// </summary>
        /// <param name="methodInfo">The <see cref="MethodInfo"/>.</param>
        /// <returns><c>true</c> if the <paramref name="methodInfo"/> is a handler. Otherwise <c>false</c>.</returns>
        /// <remarks>
        /// Override this method to provide custom logic to determine which methods are considered handlers.
        /// </remarks>
        protected virtual bool IsHandler(MethodInfo methodInfo)
        {
            // The SpecialName bit is set to flag members that are treated in a special way by some compilers
            // (such as property accessors and operator overloading methods).
            if (methodInfo.IsSpecialName)
            {
                return false;
            }

            // Overriden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
            if (methodInfo.GetBaseDefinition().DeclaringType == typeof(object))
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

            return methodInfo.IsPublic;
        }

        internal static bool TryParseHandlerMethod(string methodName, out string httpMethod, out string handler)
        {
            httpMethod = null;
            handler = null;

            // Handler method names always start with "On"
            if (!methodName.StartsWith("On") || methodName.Length <= "On".Length)
            {
                return false;
            }

            // Now we parse the method name according to our conventions to determine the required HTTP method
            // and optional 'handler name'.
            //
            // Valid names look like:
            //  - OnGet
            //  - OnPost
            //  - OnFooBar
            //  - OnTraceAsync
            //  - OnPostEditAsync

            var start = "On".Length;
            var length = methodName.Length;
            if (methodName.EndsWith("Async", StringComparison.Ordinal))
            {
                length -= "Async".Length;
            }

            if (start == length)
            {
                // There are no additional characters. This is "On" or "OnAsync".
                return false;
            }

            // The http method follows "On" and is required to be at least one character. We use casing
            // to determine where it ends.
            var handlerNameStart = start + 1;
            for (; handlerNameStart < length; handlerNameStart++)
            {
                if (char.IsUpper(methodName[handlerNameStart]))
                {
                    break;
                }
            }

            httpMethod = methodName.Substring(start, handlerNameStart - start);

            // The handler name follows the http method and is optional. It includes everything up to the end
            // excluding the "Async" suffix (if present).
            handler = handlerNameStart == length ? null : methodName.Substring(handlerNameStart, length - handlerNameStart);
            return true;
        }
    }
}
