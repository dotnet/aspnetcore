// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

#pragma warning disable CA1852 // Seal internal types
internal class DefaultApplicationModelProvider : IApplicationModelProvider
#pragma warning restore CA1852 // Seal internal types
{
    private readonly MvcOptions _mvcOptions;
    private readonly IModelMetadataProvider _modelMetadataProvider;
    private readonly Func<ActionContext, bool> _supportsAllRequests;
    private readonly Func<ActionContext, bool> _supportsNonGetRequests;

    public DefaultApplicationModelProvider(
        IOptions<MvcOptions> mvcOptionsAccessor,
        IModelMetadataProvider modelMetadataProvider)
    {
        _mvcOptions = mvcOptionsAccessor.Value;
        _modelMetadataProvider = modelMetadataProvider;

        _supportsAllRequests = _ => true;
        _supportsNonGetRequests = context => !HttpMethods.IsGet(context.HttpContext.Request.Method);
    }

    /// <inheritdoc />
    public int Order => -1000;

    /// <inheritdoc />
    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var filter in _mvcOptions.Filters)
        {
            context.Result.Filters.Add(filter);
        }

        foreach (var controllerType in context.ControllerTypes)
        {
            var controllerModel = CreateControllerModel(controllerType);
            if (controllerModel == null)
            {
                continue;
            }

            context.Result.Controllers.Add(controllerModel);
            controllerModel.Application = context.Result;

            foreach (var propertyHelper in PropertyHelper.GetProperties(controllerType.AsType()))
            {
                var propertyInfo = propertyHelper.Property;
                var propertyModel = CreatePropertyModel(propertyInfo);
                if (propertyModel != null)
                {
                    propertyModel.Controller = controllerModel;
                    controllerModel.ControllerProperties.Add(propertyModel);
                }
            }

            foreach (var methodInfo in controllerType.AsType().GetMethods())
            {
                var actionModel = CreateActionModel(controllerType, methodInfo);
                if (actionModel == null)
                {
                    continue;
                }

                actionModel.Controller = controllerModel;
                controllerModel.Actions.Add(actionModel);

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
        }
    }

    /// <inheritdoc />
    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    {
        // Intentionally empty.
    }

    /// <summary>
    /// Creates a <see cref="ControllerModel"/> for the given <see cref="TypeInfo"/>.
    /// </summary>
    /// <param name="typeInfo">The <see cref="TypeInfo"/>.</param>
    /// <returns>A <see cref="ControllerModel"/> for the given <see cref="TypeInfo"/>.</returns>
    internal static ControllerModel CreateControllerModel(TypeInfo typeInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        // For attribute routes on a controller, we want to support 'overriding' routes on a derived
        // class. So we need to walk up the hierarchy looking for the first class to define routes.
        //
        // Then we want to 'filter' the set of attributes, so that only the effective routes apply.
        var currentTypeInfo = typeInfo;
        var objectTypeInfo = typeof(object).GetTypeInfo();

        IRouteTemplateProvider[] routeAttributes;

        do
        {
            routeAttributes = currentTypeInfo
                .GetCustomAttributes(inherit: false)
                .OfType<IRouteTemplateProvider>()
                .ToArray();

            if (routeAttributes.Length > 0)
            {
                // Found 1 or more route attributes.
                break;
            }

            currentTypeInfo = currentTypeInfo.BaseType!.GetTypeInfo();
        }
        while (currentTypeInfo != objectTypeInfo);

        var attributes = typeInfo.GetCustomAttributes(inherit: true);

        // This is fairly complicated so that we maintain referential equality between items in
        // ControllerModel.Attributes and ControllerModel.Attributes[*].Attribute.
        var filteredAttributes = new List<object>();
        foreach (var attribute in attributes)
        {
            if (attribute is IRouteTemplateProvider)
            {
                // This attribute is a route-attribute, leave it out.
            }
            else
            {
                filteredAttributes.Add(attribute);
            }
        }

        filteredAttributes.AddRange(routeAttributes);

        attributes = filteredAttributes.ToArray();

        var controllerModel = new ControllerModel(typeInfo, attributes);

        AddRange(controllerModel.Selectors, CreateSelectors(attributes));

        controllerModel.ControllerName =
            typeInfo.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) ?
                typeInfo.Name.Substring(0, typeInfo.Name.Length - "Controller".Length) :
                typeInfo.Name;

        AddRange(controllerModel.Filters, attributes.OfType<IFilterMetadata>());

        foreach (var routeValueProvider in attributes.OfType<IRouteValueProvider>())
        {
            controllerModel.RouteValues.Add(routeValueProvider.RouteKey, routeValueProvider.RouteValue);
        }

        var apiVisibility = attributes.OfType<IApiDescriptionVisibilityProvider>().FirstOrDefault();
        if (apiVisibility != null)
        {
            controllerModel.ApiExplorer.IsVisible = !apiVisibility.IgnoreApi;
        }

        var apiGroupName = attributes.OfType<IApiDescriptionGroupNameProvider>().FirstOrDefault();
        if (apiGroupName != null)
        {
            controllerModel.ApiExplorer.GroupName = apiGroupName.GroupName;
        }

        // Controllers can implement action filter and result filter interfaces. We add
        // a special delegating filter implementation to the pipeline to handle it.
        //
        // This is needed because filters are instantiated before the controller.
        if (typeof(IAsyncActionFilter).GetTypeInfo().IsAssignableFrom(typeInfo) ||
            typeof(IActionFilter).GetTypeInfo().IsAssignableFrom(typeInfo))
        {
            controllerModel.Filters.Add(new ControllerActionFilter());
        }
        if (typeof(IAsyncResultFilter).GetTypeInfo().IsAssignableFrom(typeInfo) ||
            typeof(IResultFilter).GetTypeInfo().IsAssignableFrom(typeInfo))
        {
            controllerModel.Filters.Add(new ControllerResultFilter());
        }

        return controllerModel;
    }

    /// <summary>
    /// Creates a <see cref="PropertyModel"/> for the given <see cref="PropertyInfo"/>.
    /// </summary>
    /// <param name="propertyInfo">The <see cref="PropertyInfo"/>.</param>
    /// <returns>A <see cref="PropertyModel"/> for the given <see cref="PropertyInfo"/>.</returns>
    internal PropertyModel CreatePropertyModel(PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);

        var attributes = propertyInfo.GetCustomAttributes(inherit: true);

        // BindingInfo for properties can be either specified by decorating the property with binding specific attributes.
        // ModelMetadata also adds information from the property's type and any configured IBindingMetadataProvider.
        var declaringType = propertyInfo.DeclaringType!;
        var modelMetadata = _modelMetadataProvider.GetMetadataForProperty(declaringType, propertyInfo.Name);
        var bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);

        if (bindingInfo == null)
        {
            // Look for BindPropertiesAttribute on the handler type if no BindingInfo was inferred for the property.
            // This allows a user to enable model binding on properties by decorating the controller type with BindPropertiesAttribute.
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

        var propertyModel = new PropertyModel(propertyInfo, attributes)
        {
            PropertyName = propertyInfo.Name,
            BindingInfo = bindingInfo,
        };

        return propertyModel;
    }

    /// <summary>
    /// Creates the <see cref="ActionModel"/> instance for the given action <see cref="MethodInfo"/>.
    /// </summary>
    /// <param name="typeInfo">The controller <see cref="TypeInfo"/>.</param>
    /// <param name="methodInfo">The action <see cref="MethodInfo"/>.</param>
    /// <returns>
    /// An <see cref="ActionModel"/> instance for the given action <see cref="MethodInfo"/> or
    /// <c>null</c> if the <paramref name="methodInfo"/> does not represent an action.
    /// </returns>
    internal ActionModel? CreateActionModel(
        TypeInfo typeInfo,
        MethodInfo methodInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        ArgumentNullException.ThrowIfNull(methodInfo);

        if (!IsAction(typeInfo, methodInfo))
        {
            return null;
        }

        var attributes = methodInfo.GetCustomAttributes(inherit: true);

        var actionModel = new ActionModel(methodInfo, attributes);

        AddRange(actionModel.Filters, attributes.OfType<IFilterMetadata>());

        var actionName = attributes.OfType<ActionNameAttribute>().FirstOrDefault();
        if (actionName?.Name != null)
        {
            actionModel.ActionName = actionName.Name;
        }
        else
        {
            actionModel.ActionName = CanonicalizeActionName(methodInfo.Name);
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

        foreach (var routeValueProvider in attributes.OfType<IRouteValueProvider>())
        {
            actionModel.RouteValues.Add(routeValueProvider.RouteKey, routeValueProvider.RouteValue);
        }

        // Now we need to determine the action selection info (cross-section of routes and constraints)
        //
        // For attribute routes on a action, we want to support 'overriding' routes on a
        // virtual method, but allow 'overriding'. So we need to walk up the hierarchy looking
        // for the first definition to define routes.
        //
        // Then we want to 'filter' the set of attributes, so that only the effective routes apply.
        var currentMethodInfo = methodInfo;

        IRouteTemplateProvider[] routeAttributes;

        while (true)
        {
            routeAttributes = currentMethodInfo
                .GetCustomAttributes(inherit: false)
                .OfType<IRouteTemplateProvider>()
                .ToArray();

            if (routeAttributes.Length > 0)
            {
                // Found 1 or more route attributes.
                break;
            }

            // GetBaseDefinition returns 'this' when it gets to the bottom of the chain.
            var nextMethodInfo = currentMethodInfo.GetBaseDefinition();
            if (currentMethodInfo == nextMethodInfo)
            {
                break;
            }

            currentMethodInfo = nextMethodInfo;
        }

        // This is fairly complicated so that we maintain referential equality between items in
        // ActionModel.Attributes and ActionModel.Attributes[*].Attribute.
        var applicableAttributes = new List<object>(routeAttributes.Length);
        foreach (var attribute in attributes)
        {
            if (attribute is IRouteTemplateProvider)
            {
                // This attribute is a route-attribute, leave it out.
            }
            else
            {
                applicableAttributes.Add(attribute);
            }
        }

        applicableAttributes.AddRange(routeAttributes);
        AddRange(actionModel.Selectors, CreateSelectors(applicableAttributes));

        AddReturnTypeMetadata(actionModel.Selectors, methodInfo);

        return actionModel;
    }

    internal static void AddReturnTypeMetadata(IList<SelectorModel> selectors, MethodInfo methodInfo)
    {
        // Get metadata from return type
        var returnType = methodInfo.ReturnType;
        if (CoercedAwaitableInfo.IsTypeAwaitable(returnType, out var coercedAwaitableInfo))
        {
            returnType = coercedAwaitableInfo.AwaitableInfo.ResultType;
        }

        if (returnType is not null && typeof(IEndpointMetadataProvider).IsAssignableFrom(returnType))
        {
            // Return type implements IEndpointMetadataProvider
            var builder = new InertEndpointBuilder();
            var invokeArgs = new object[2];
            invokeArgs[0] = methodInfo;
            invokeArgs[1] = builder;
            EndpointMetadataPopulator.PopulateMetadataForEndpointMethod.MakeGenericMethod(returnType).Invoke(null, invokeArgs);

            // The metadata is added to the builder's metadata collection.
            // We need to populate the selectors with that metadata.
            foreach (var metadata in builder.Metadata)
            {
                foreach (var selector in selectors)
                {
                    selector.EndpointMetadata.Add(metadata);
                }
            }
        }
    }

    private string CanonicalizeActionName(string actionName)
    {
        const string Suffix = "Async";

        if (_mvcOptions.SuppressAsyncSuffixInActionNames &&
            actionName.EndsWith(Suffix, StringComparison.Ordinal))
        {
            actionName = actionName.Substring(0, actionName.Length - Suffix.Length);
        }

        return actionName;
    }

    /// <summary>
    /// Returns <c>true</c> if the <paramref name="methodInfo"/> is an action. Otherwise <c>false</c>.
    /// </summary>
    /// <param name="typeInfo">The <see cref="TypeInfo"/>.</param>
    /// <param name="methodInfo">The <see cref="MethodInfo"/>.</param>
    /// <returns><c>true</c> if the <paramref name="methodInfo"/> is an action. Otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Override this method to provide custom logic to determine which methods are considered actions.
    /// </remarks>
    internal static bool IsAction(TypeInfo typeInfo, MethodInfo methodInfo)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);
        ArgumentNullException.ThrowIfNull(methodInfo);

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

        // Overridden methods from Object class, e.g. Equals(Object), GetHashCode(), etc., are not valid.
        if (methodInfo.GetBaseDefinition().DeclaringType == typeof(object))
        {
            return false;
        }

        // Dispose method implemented from IDisposable is not valid
        if (IsIDisposableMethod(methodInfo))
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

    /// <summary>
    /// Creates a <see cref="ParameterModel"/> for the given <see cref="ParameterInfo"/>.
    /// </summary>
    /// <param name="parameterInfo">The <see cref="ParameterInfo"/>.</param>
    /// <returns>A <see cref="ParameterModel"/> for the given <see cref="ParameterInfo"/>.</returns>
    internal ParameterModel CreateParameterModel(ParameterInfo parameterInfo)
    {
        ArgumentNullException.ThrowIfNull(parameterInfo);

        var attributes = parameterInfo.GetCustomAttributes(inherit: true);

        BindingInfo? bindingInfo;
        if (_modelMetadataProvider is ModelMetadataProvider modelMetadataProviderBase)
        {
            var modelMetadata = modelMetadataProviderBase.GetMetadataForParameter(parameterInfo);
            bindingInfo = BindingInfo.GetBindingInfo(attributes, modelMetadata);
        }
        else
        {
            // GetMetadataForParameter should only be used if the user has opted in to the 2.1 behavior.
            bindingInfo = BindingInfo.GetBindingInfo(attributes);
        }

        var parameterModel = new ParameterModel(parameterInfo, attributes)
        {
            ParameterName = parameterInfo.Name!,
            BindingInfo = bindingInfo,
        };

        return parameterModel;
    }

    private static IList<SelectorModel> CreateSelectors(IList<object> attributes)
    {
        // Route attributes create multiple selector models, we want to split the set of
        // attributes based on these so each selector only has the attributes that affect it.
        //
        // The set of route attributes are split into those that 'define' a route versus those that are
        // 'silent'.
        //
        // We need to define a selector for each attribute that 'defines' a route, and a single selector
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
        // This will generate 2 selectors:
        // 1. [HttpPost("Api/Things")]
        // 2. [HttpGet], [AcceptVerbs("POST", "PUT")]
        //
        // Another example of this situation is:
        //
        // [Route("api/Products")]
        // [AcceptVerbs("GET", "HEAD")]
        // [HttpPost("api/Products/new")]
        //
        // This will generate 2 selectors:
        // 1. [AcceptVerbs("GET", "HEAD")]
        // 2. [HttpPost]
        //
        // Note that having a route attribute that doesn't define a route template _might_ be an error. We
        // don't have enough context to really know at this point so we just pass it on.
        var routeProviders = new List<IRouteTemplateProvider>();

        var createSelectorForSilentRouteProviders = false;
        foreach (var attribute in attributes)
        {
            if (attribute is IRouteTemplateProvider routeTemplateProvider)
            {
                if (IsSilentRouteAttribute(routeTemplateProvider))
                {
                    createSelectorForSilentRouteProviders = true;
                }
                else
                {
                    routeProviders.Add(routeTemplateProvider);
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
            // Is two selectors. And...
            //
            // [HttpGet]
            // [Route("Products")]
            // public void Foo() { }
            //
            // Is one selector.
            if (!(routeProvider is IActionHttpMethodProvider))
            {
                createSelectorForSilentRouteProviders = false;
                break;
            }
        }

        var selectorModels = new List<SelectorModel>();
        if (routeProviders.Count == 0 && !createSelectorForSilentRouteProviders)
        {
            // Simple case, all attributes apply
            selectorModels.Add(CreateSelectorModel(route: null, attributes: attributes));
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
                    if (ReferenceEquals(attribute, routeProvider))
                    {
                        filteredAttributes.Add(attribute);
                    }
                    else if (InRouteProviders(routeProviders, attribute))
                    {
                        // Exclude other route template providers
                        // Example:
                        // [HttpGet("template")]
                        // [Route("template/{id}")]
                    }
                    else if (
                        routeProvider is IActionHttpMethodProvider &&
                        attribute is IActionHttpMethodProvider)
                    {
                        // Example:
                        // [HttpGet("template")]
                        // [AcceptVerbs("GET", "POST")]
                        //
                        // Exclude other http method providers if this route is an
                        // http method provider.
                    }
                    else
                    {
                        filteredAttributes.Add(attribute);
                    }
                }

                selectorModels.Add(CreateSelectorModel(routeProvider, filteredAttributes));
            }

            if (createSelectorForSilentRouteProviders)
            {
                var filteredAttributes = new List<object>();
                foreach (var attribute in attributes)
                {
                    if (!InRouteProviders(routeProviders, attribute))
                    {
                        filteredAttributes.Add(attribute);
                    }
                }

                selectorModels.Add(CreateSelectorModel(route: null, attributes: filteredAttributes));
            }
        }

        return selectorModels;
    }

    private static bool InRouteProviders(List<IRouteTemplateProvider> routeProviders, object attribute)
    {
        foreach (var rp in routeProviders)
        {
            if (ReferenceEquals(rp, attribute))
            {
                return true;
            }
        }

        return false;
    }

    private static SelectorModel CreateSelectorModel(IRouteTemplateProvider? route, IList<object> attributes)
    {
        var selectorModel = new SelectorModel();
        if (route != null)
        {
            selectorModel.AttributeRouteModel = new AttributeRouteModel(route);
        }

        AddRange(selectorModel.ActionConstraints, attributes.OfType<IActionConstraintMetadata>());
        AddRange(selectorModel.EndpointMetadata, attributes);

        // Simple case, all HTTP method attributes apply
        var httpMethods = attributes
            .OfType<IActionHttpMethodProvider>()
            .SelectMany(a => a.HttpMethods)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (httpMethods.Length > 0)
        {
            selectorModel.ActionConstraints.Add(new HttpMethodActionConstraint(httpMethods));
            selectorModel.EndpointMetadata.Add(new HttpMethodMetadata(httpMethods));
        }

        return selectorModel;
    }

    private static bool IsIDisposableMethod(MethodInfo methodInfo)
    {
        // Ideally we do not want Dispose method to be exposed as an action. However there are some scenarios where a user
        // might want to expose a method with name "Dispose" (even though they might not be really disposing resources)
        // Example: A controller deriving from MVC's Controller type might wish to have a method with name Dispose,
        // in which case they can use the "new" keyword to hide the base controller's declaration.

        // Find where the method was originally declared
        var baseMethodInfo = methodInfo.GetBaseDefinition();
        var declaringType = baseMethodInfo.DeclaringType;

        return
            (typeof(IDisposable).IsAssignableFrom(declaringType) &&
             declaringType.GetInterfaceMap(typeof(IDisposable)).TargetMethods[0] == baseMethodInfo);
    }

    private static bool IsSilentRouteAttribute(IRouteTemplateProvider routeTemplateProvider)
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
