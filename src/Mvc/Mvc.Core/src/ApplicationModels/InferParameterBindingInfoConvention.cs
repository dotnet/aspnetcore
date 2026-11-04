// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Resources = Microsoft.AspNetCore.Mvc.Core.Resources;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// An <see cref="IActionModelConvention"/> that infers <see cref="BindingInfo.BindingSource"/> for parameters.
/// </summary>
/// <remarks>
/// The goal of this convention is to make intuitive and easy to document <see cref="BindingSource"/> inferences. The rules are:
/// <list type="number">
/// <item>A previously specified <see cref="BindingInfo.BindingSource" /> is never overwritten.</item>
/// <item>A complex type parameter (<see cref="ModelMetadata.IsComplexType"/>), registered in the DI container, is assigned <see cref="BindingSource.Services"/>.</item>
/// <item>A complex type parameter (<see cref="ModelMetadata.IsComplexType"/>), not registered in the DI container, is assigned <see cref="BindingSource.Body"/>.</item>
/// <item>Parameter with a name that appears as a route value in ANY route template is assigned <see cref="BindingSource.Path"/>.</item>
/// <item>All other parameters are <see cref="BindingSource.Query"/>.</item>
/// </list>
/// </remarks>
public class InferParameterBindingInfoConvention : IActionModelConvention
{
    private readonly IModelMetadataProvider _modelMetadataProvider;
    private readonly IServiceProviderIsService? _serviceProviderIsService;

    /// <summary>
    /// Initializes a new instance of <see cref="InferParameterBindingInfoConvention"/>.
    /// </summary>
    /// <param name="modelMetadataProvider">The model metadata provider.</param>
    public InferParameterBindingInfoConvention(
        IModelMetadataProvider modelMetadataProvider)
    {
        _modelMetadataProvider = modelMetadataProvider ?? throw new ArgumentNullException(nameof(modelMetadataProvider));
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InferParameterBindingInfoConvention"/>.
    /// </summary>
    /// <param name="modelMetadataProvider">The model metadata provider.</param>
    /// <param name="serviceProviderIsService">The service to determine if the a type is available from the <see cref="IServiceProvider"/>.</param>
    public InferParameterBindingInfoConvention(
        IModelMetadataProvider modelMetadataProvider,
        IServiceProviderIsService serviceProviderIsService)
        : this(modelMetadataProvider)
    {
        _serviceProviderIsService = serviceProviderIsService ?? throw new ArgumentNullException(nameof(serviceProviderIsService));
    }

    internal bool IsInferForServiceParametersEnabled => _serviceProviderIsService != null;

    /// <summary>
    /// Called to determine whether the action should apply.
    /// </summary>
    /// <param name="action">The action in question.</param>
    /// <returns><see langword="true"/> if the action should apply.</returns>
    protected virtual bool ShouldApply(ActionModel action) => true;

    /// <summary>
    /// Called to apply the convention to the <see cref="ActionModel"/>.
    /// </summary>
    /// <param name="action">The <see cref="ActionModel"/>.</param>
    public void Apply(ActionModel action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!ShouldApply(action))
        {
            return;
        }

        InferParameterBindingSources(action);
    }

    internal void InferParameterBindingSources(ActionModel action)
    {
        for (var i = 0; i < action.Parameters.Count; i++)
        {
            var parameter = action.Parameters[i];
            var bindingSource = parameter.BindingInfo?.BindingSource;
            if (bindingSource == null)
            {
                parameter.BindingInfo ??= new BindingInfo();
                parameter.BindingInfo.BindingSource = InferBindingSourceForParameter(parameter);
            }
        }

        var fromBodyParameters = action.Parameters.Where(p => p.BindingInfo!.BindingSource == BindingSource.Body).ToList();
        if (fromBodyParameters.Count > 1)
        {
            var parameters = string.Join(Environment.NewLine, fromBodyParameters.Select(p => p.DisplayName));
            var message = Resources.FormatApiController_MultipleBodyParametersFound(
                action.DisplayName,
                nameof(FromQueryAttribute),
                nameof(FromRouteAttribute),
                nameof(FromBodyAttribute));

            message += Environment.NewLine + parameters;
            throw new InvalidOperationException(message);
        }
        else if (fromBodyParameters.Count == 1 &&
                  fromBodyParameters[0].BindingInfo!.EmptyBodyBehavior == EmptyBodyBehavior.Default &&
                  IsOptionalParameter(fromBodyParameters[0]))
        {
            fromBodyParameters[0].BindingInfo!.EmptyBodyBehavior = EmptyBodyBehavior.Allow;
        }
    }

    // Internal for unit testing.
    internal BindingSource? InferBindingSourceForParameter(ParameterModel parameter)
    {
        if (IsComplexTypeParameter(parameter, out var metadata))
        {
            if (IsService(parameter.ParameterType))
            {
                return BindingSource.Services;
            }

            return metadata.BoundProperties.Any(prop => prop.BindingSource is not null) ? null : BindingSource.Body;
        }

        if (ParameterExistsInAnyRoute(parameter.Action, parameter.ParameterName))
        {
            return BindingSource.Path;
        }

        return BindingSource.Query;
    }

    private bool IsService(Type type)
    {
        if (_serviceProviderIsService == null)
        {
            return false;
        }

        // IServiceProviderIsService will special case IEnumerable<> and always return true
        // so, in this case checking the element type instead
        if (type.IsConstructedGenericType &&
            type.GetGenericTypeDefinition() is Type genericDefinition &&
            genericDefinition == typeof(IEnumerable<>))
        {
            type = type.GenericTypeArguments[0];
        }

        return _serviceProviderIsService.IsService(type);
    }

    private static bool ParameterExistsInAnyRoute(ActionModel action, string parameterName)
    {
        foreach (var selector in ActionAttributeRouteModel.FlattenSelectors(action))
        {
            if (selector.AttributeRouteModel == null)
            {
                continue;
            }

            var parsedTemplate = TemplateParser.Parse(selector.AttributeRouteModel.Template!);
            if (parsedTemplate.GetParameter(parameterName) != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsComplexTypeParameter(ParameterModel parameter, out ModelMetadata metadata)
    {
        // No need for information from attributes on the parameter. Just use its type.
        metadata = _modelMetadataProvider.GetMetadataForType(parameter.ParameterInfo.ParameterType);

        return metadata.IsComplexType;
    }

    private bool IsOptionalParameter(ParameterModel parameter)
    {
        if (parameter.ParameterInfo.HasDefaultValue)
        {
            return true;
        }

        if (_modelMetadataProvider is ModelMetadataProvider modelMetadataProvider)
        {
            var metadata = modelMetadataProvider.GetMetadataForParameter(parameter.ParameterInfo);
            return metadata.NullabilityState == NullabilityState.Nullable || metadata.IsNullableValueType;
        }
        else
        {
            // Cannot be determine if the parameter is optional since the provider
            // does not provides an option to getMetadata from the parameter info
            // so, we will NOT treat the parameter as optional.
            return false;
        }
    }
}
