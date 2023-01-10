// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// Applies conventions to a <see cref="ApplicationModel"/>.
/// </summary>
internal static class ApplicationModelConventions
{
    /// <summary>
    /// Applies conventions to a <see cref="ApplicationModel"/>.
    /// </summary>
    /// <param name="applicationModel">The <see cref="ApplicationModel"/>.</param>
    /// <param name="conventions">The set of conventions.</param>
    public static void ApplyConventions(
        ApplicationModel applicationModel,
        IEnumerable<IApplicationModelConvention> conventions)
    {
        ArgumentNullException.ThrowIfNull(applicationModel);
        ArgumentNullException.ThrowIfNull(conventions);

        // Conventions are applied from the outside-in to allow for scenarios where an action overrides
        // a controller, etc.
        foreach (var convention in conventions)
        {
            convention.Apply(applicationModel);
        }

        var controllers = applicationModel.Controllers.ToArray();
        // First apply the conventions from attributes in decreasing order of scope.
        foreach (var controller in controllers)
        {
            // ToArray is needed here to prevent issues with modifying the attributes collection
            // while iterating it.
            var controllerConventions =
                controller.Attributes
                    .OfType<IControllerModelConvention>()
                    .ToArray();

            foreach (var controllerConvention in controllerConventions)
            {
                controllerConvention.Apply(controller);
            }

            var actions = controller.Actions.ToArray();
            foreach (var action in actions)
            {
                // ToArray is needed here to prevent issues with modifying the attributes collection
                // while iterating it.
                var actionConventions =
                    action.Attributes
                        .OfType<IActionModelConvention>()
                        .ToArray();

                foreach (var actionConvention in actionConventions)
                {
                    actionConvention.Apply(action);
                }

                var parameters = action.Parameters.ToArray();
                foreach (var parameter in parameters)
                {
                    // ToArray is needed here to prevent issues with modifying the attributes collection
                    // while iterating it.
                    var parameterConventions =
                        parameter.Attributes
                            .OfType<IParameterModelConvention>()
                            .ToArray();

                    foreach (var parameterConvention in parameterConventions)
                    {
                        parameterConvention.Apply(parameter);
                    }

                    var parameterBaseConventions = GetConventions<IParameterModelBaseConvention>(conventions, parameter.Attributes);
                    foreach (var parameterConvention in parameterBaseConventions)
                    {
                        parameterConvention.Apply(parameter);
                    }
                }
            }

            var properties = controller.ControllerProperties.ToArray();
            foreach (var property in properties)
            {
                var parameterBaseConventions = GetConventions<IParameterModelBaseConvention>(conventions, property.Attributes);

                foreach (var parameterConvention in parameterBaseConventions)
                {
                    parameterConvention.Apply(property);
                }
            }
        }
    }

    private static IEnumerable<TConvention> GetConventions<TConvention>(
        IEnumerable<IApplicationModelConvention> conventions,
        IReadOnlyList<object> attributes)
    {
        return Enumerable.Concat(
            conventions.OfType<TConvention>(),
            attributes.OfType<TConvention>());
    }
}
