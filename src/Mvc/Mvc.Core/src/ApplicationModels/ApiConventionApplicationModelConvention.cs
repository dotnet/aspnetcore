// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// An <see cref="IActionModelConvention"/> that discovers
/// <list type="bullet">
/// <item><description><see cref="ApiConventionResult"/> from applied <see cref="ApiConventionTypeAttribute"/> or <see cref="ApiConventionMethodAttribute"/>.</description></item>
/// <item><description><see cref="ProducesErrorResponseTypeAttribute"/> that applies to the action.</description></item>
/// </list>
/// </summary>
public class ApiConventionApplicationModelConvention : IActionModelConvention
{
    /// <summary>
    /// Initializes a new instance of <see cref="ApiConventionApplicationModelConvention"/>.
    /// </summary>
    /// <param name="defaultErrorResponseType">The error type to be used. Use <see cref="void" />
    /// when no default error type is to be inferred.
    /// </param>
    public ApiConventionApplicationModelConvention(ProducesErrorResponseTypeAttribute defaultErrorResponseType)
    {
        DefaultErrorResponseType = defaultErrorResponseType ?? throw new ArgumentNullException(nameof(defaultErrorResponseType));
    }

    /// <summary>
    /// Gets the default <see cref="ProducesErrorResponseTypeAttribute"/> that is associated with an action
    /// when no attribute is discovered.
    /// </summary>
    public ProducesErrorResponseTypeAttribute DefaultErrorResponseType { get; }

    /// <inheritdoc />
    public void Apply(ActionModel action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!ShouldApply(action))
        {
            return;
        }

        DiscoverApiConvention(action);
        DiscoverErrorResponseType(action);
    }

    /// <summary>
    /// Determines if this instance of <see cref="IActionModelConvention"/> applies to a specified <paramref name="action"/>.
    /// </summary>
    /// <param name="action">The <see cref="ActionModel"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the convention applies, otherwise <see langword="false"/>.
    /// Derived types may override this method to selectively apply this convention.
    /// </returns>
    protected virtual bool ShouldApply(ActionModel action) => true;

    private static void DiscoverApiConvention(ActionModel action)
    {
        var controller = action.Controller;
        var apiConventionAttributes = controller.Attributes.OfType<ApiConventionTypeAttribute>().ToArray();
        if (apiConventionAttributes.Length == 0)
        {
            var controllerAssembly = controller.ControllerType.Assembly;
            apiConventionAttributes = controllerAssembly.GetCustomAttributes<ApiConventionTypeAttribute>().ToArray();
        }

        if (ApiConventionResult.TryGetApiConvention(action.ActionMethod, apiConventionAttributes, out var result))
        {
            action.Properties[typeof(ApiConventionResult)] = result;
        }
    }

    private void DiscoverErrorResponseType(ActionModel action)
    {
        var errorTypeAttribute =
            action.Attributes.OfType<ProducesErrorResponseTypeAttribute>().FirstOrDefault() ??
            action.Controller.Attributes.OfType<ProducesErrorResponseTypeAttribute>().FirstOrDefault() ??
            action.Controller.ControllerType.Assembly.GetCustomAttribute<ProducesErrorResponseTypeAttribute>() ??
            DefaultErrorResponseType;

        action.Properties[typeof(ProducesErrorResponseTypeAttribute)] = errorTypeAttribute;
    }
}
