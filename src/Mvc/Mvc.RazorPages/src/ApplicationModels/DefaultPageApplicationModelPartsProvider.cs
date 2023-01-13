// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

internal sealed class DefaultPageApplicationModelPartsProvider : IPageApplicationModelPartsProvider
{
    private readonly IModelMetadataProvider _modelMetadataProvider;

    private readonly Func<ActionContext, bool> _supportsAllRequests;
    private readonly Func<ActionContext, bool> _supportsNonGetRequests;

    public DefaultPageApplicationModelPartsProvider(IModelMetadataProvider modelMetadataProvider)
    {
        _modelMetadataProvider = modelMetadataProvider;

        _supportsAllRequests = _ => true;
        _supportsNonGetRequests = context => !HttpMethods.IsGet(context.HttpContext.Request.Method);
    }

    /// <summary>
    /// Creates a <see cref="PageHandlerModel"/> for the specified <paramref name="method"/>.s
    /// </summary>
    /// <param name="method">The <see cref="MethodInfo"/>.</param>
    /// <returns>The <see cref="PageHandlerModel"/>.</returns>
    public PageHandlerModel? CreateHandlerModel(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        if (!IsHandler(method))
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
    public PageParameterModel CreateParameterModel(ParameterInfo parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        var attributes = parameter.GetCustomAttributes(inherit: true);

        BindingInfo? bindingInfo;
        if (_modelMetadataProvider is ModelMetadataProvider modelMetadataProviderBase)
        {
            var modelMetadata = modelMetadataProviderBase.GetMetadataForParameter(parameter);
            bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);
        }
        else
        {
            bindingInfo = BindingInfo.GetBindingInfo(attributes);
        }

        return new PageParameterModel(parameter, attributes)
        {
            BindingInfo = bindingInfo,
            ParameterName = parameter.Name!,
        };
    }

    /// <summary>
    /// Creates a <see cref="PagePropertyModel"/> for the <paramref name="property"/>.
    /// </summary>
    /// <param name="property">The <see cref="PropertyInfo"/>.</param>
    /// <returns>The <see cref="PagePropertyModel"/>.</returns>
    public PagePropertyModel CreatePropertyModel(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);

        var propertyAttributes = property.GetCustomAttributes(inherit: true);

        // BindingInfo for properties can be either specified by decorating the property with binding-specific attributes.
        // ModelMetadata also adds information from the property's type and any configured IBindingMetadataProvider.
        var propertyMetadata = _modelMetadataProvider.GetMetadataForProperty(property.DeclaringType!, property.Name);
        var bindingInfo = BindingInfo.GetBindingInfo(propertyAttributes, propertyMetadata);

        if (bindingInfo == null)
        {
            // Look for BindPropertiesAttribute on the handler type if no BindingInfo was inferred for the property.
            // This allows a user to enable model binding on properties by decorating the controller type with BindPropertiesAttribute.
            var declaringType = property.DeclaringType!;
            var bindPropertiesAttribute = declaringType.GetCustomAttribute<BindPropertiesAttribute>(inherit: true);
            if (bindPropertiesAttribute != null)
            {
                var requestPredicate = bindPropertiesAttribute.SupportsGet ? _supportsAllRequests : _supportsNonGetRequests;
                bindingInfo = new BindingInfo
                {
                    RequestPredicate = requestPredicate,
                };
            }
        }

        var model = new PagePropertyModel(property, propertyAttributes)
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
    public bool IsHandler(MethodInfo methodInfo)
    {
        // The SpecialName bit is set to flag members that are treated in a special way by some compilers
        // (such as property accessors and operator overloading methods).
        if (methodInfo.IsSpecialName)
        {
            return false;
        }

        // Overridden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
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

        if (!methodInfo.IsPublic)
        {
            return false;
        }

        if (methodInfo.IsDefined(typeof(NonHandlerAttribute)))
        {
            return false;
        }

        // Exclude the whole hierarchy of Page.
        var declaringType = methodInfo.DeclaringType;
        if (declaringType == typeof(Page) ||
            declaringType == typeof(PageBase) ||
            declaringType == typeof(RazorPageBase))
        {
            return false;
        }

        // Exclude methods declared on PageModel
        if (declaringType == typeof(PageModel))
        {
            return false;
        }

        return true;
    }

    internal static bool TryParseHandlerMethod(string methodName, [NotNullWhen(true)] out string? httpMethod, out string? handler)
    {
        httpMethod = null;
        handler = null;

        // Handler method names always start with "On"
        if (!methodName.StartsWith("On", StringComparison.Ordinal) || methodName.Length <= "On".Length)
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
