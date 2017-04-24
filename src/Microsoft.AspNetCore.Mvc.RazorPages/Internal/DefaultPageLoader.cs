// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class DefaultPageLoader : IPageLoader
    {
        private const string ModelPropertyName = "Model";
        
        private readonly RazorCompiler _compiler;

        public DefaultPageLoader(RazorCompiler compiler)
        {
            _compiler = compiler;
        }

        public CompiledPageActionDescriptor Load(PageActionDescriptor actionDescriptor)
        {
            var result = _compiler.Compile(actionDescriptor.RelativePath);
            return CreateDescriptor(actionDescriptor, result.CompiledType.GetTypeInfo());
        }

        // Internal for unit testing
        internal static CompiledPageActionDescriptor CreateDescriptor(PageActionDescriptor actionDescriptor, TypeInfo pageType)
        {
            // Pages always have a model type. If it's not set explicitly by the developer using
            // @model, it will be the same as the page type.
            //
            // However, we allow it to be null here for ease of testing.
            var modelType = pageType.GetProperty(ModelPropertyName)?.PropertyType.GetTypeInfo();

            // Now we want to find the handler methods. If the model defines any handlers, then we'll use those,
            // otherwise look at the page itself (unless the page IS the model, in which case we already looked).
            TypeInfo handlerType;

            var handlerMethods = modelType == null ? null : CreateHandlerMethods(modelType);
            if (handlerMethods?.Length > 0)
            {
                handlerType = modelType;
            }
            else
            {
                handlerType = pageType;
                handlerMethods = CreateHandlerMethods(pageType);
            }

            var boundProperties = CreateBoundProperties(handlerType);

            return new CompiledPageActionDescriptor(actionDescriptor)
            {
                ActionConstraints = actionDescriptor.ActionConstraints,
                AttributeRouteInfo = actionDescriptor.AttributeRouteInfo,
                BoundProperties = boundProperties,
                FilterDescriptors = actionDescriptor.FilterDescriptors,
                HandlerMethods = handlerMethods,
                HandlerTypeInfo = handlerType,
                ModelTypeInfo = modelType,
                RouteValues = actionDescriptor.RouteValues,
                PageTypeInfo = pageType,
                Properties = actionDescriptor.Properties,
            };
        }

        internal static HandlerMethodDescriptor[] CreateHandlerMethods(TypeInfo type)
        {
            var methods = type.GetMethods();
            var results = new List<HandlerMethodDescriptor>();

            for (var i = 0; i < methods.Length; i++)
            {
                var method = methods[i];
                if (!IsValidHandlerMethod(method))
                {
                    continue;
                }

                if (method.IsDefined(typeof(NonHandlerAttribute)))
                {
                    continue;
                }

                if (method.DeclaringType.GetTypeInfo().IsDefined(typeof(PagesBaseClassAttribute)))
                {
                    continue;
                }

                if (!TryParseHandlerMethod(method.Name, out var httpMethod, out var handler))
                {
                    continue;
                }

                var parameters = CreateHandlerParameters(method);

                var handlerMethodDescriptor = new HandlerMethodDescriptor()
                {
                    MethodInfo = method,
                    Name = handler,
                    HttpMethod = httpMethod,
                    Parameters = parameters,
                };

                results.Add(handlerMethodDescriptor);
            }

            return results.ToArray();
        }

        // Internal for testing
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

        private static bool IsValidHandlerMethod(MethodInfo methodInfo)
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

        // Internal for testing
        internal static HandlerParameterDescriptor[] CreateHandlerParameters(MethodInfo methodInfo)
        {
            var methodParameters = methodInfo.GetParameters();
            var parameters = new HandlerParameterDescriptor[methodParameters.Length];

            for (var i = 0; i < methodParameters.Length; i++)
            {
                var parameter = methodParameters[i];

                parameters[i] = new HandlerParameterDescriptor()
                {
                    BindingInfo = BindingInfo.GetBindingInfo(parameter.GetCustomAttributes()),
                    Name = parameter.Name,
                    ParameterInfo = parameter,
                    ParameterType = parameter.ParameterType,
                };
            }

            return parameters;
        }

        // Internal for testing
        internal static PageBoundPropertyDescriptor[] CreateBoundProperties(TypeInfo type)
        {
            var properties = PropertyHelper.GetVisibleProperties(type.AsType());

            // If the type has a [BindPropertyAttribute] then we'll consider any and all public properties bindable.
            var bindPropertyOnType = type.GetCustomAttribute<BindPropertyAttribute>();

            var results = new List<PageBoundPropertyDescriptor>();
            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var bindingInfo = BindingInfo.GetBindingInfo(property.Property.GetCustomAttributes());

                if (bindingInfo == null && bindPropertyOnType == null)
                {
                    continue;
                }

                if (property.Property.DeclaringType.GetTypeInfo().IsDefined(typeof(PagesBaseClassAttribute)))
                {
                    continue;
                }

                var bindPropertyOnProperty = property.Property.GetCustomAttribute<BindPropertyAttribute>();
                var supportsGet = bindPropertyOnProperty?.SupportsGet ?? bindPropertyOnType?.SupportsGet ?? false;

                var descriptor = new PageBoundPropertyDescriptor()
                {
                    BindingInfo = bindingInfo ?? new BindingInfo(),
                    Name = property.Name,
                    Property = property.Property,
                    ParameterType = property.Property.PropertyType,
                    SupportsGet = supportsGet,
                };

                results.Add(descriptor);
            }

            return results.ToArray();
        }
    }
}