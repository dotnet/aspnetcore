// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains the extension methods for <see cref="AspNetCore.Mvc.MvcOptions.Conventions"/>.
/// </summary>
public static class ApplicationModelConventionExtensions
{
    /// <summary>
    /// Removes all application model conventions of the specified type.
    /// </summary>
    /// <param name="list">The list of <see cref="IApplicationModelConvention"/>s.</param>
    /// <typeparam name="TApplicationModelConvention">The type to remove.</typeparam>
    public static void RemoveType<TApplicationModelConvention>(this IList<IApplicationModelConvention> list)
        where TApplicationModelConvention : IApplicationModelConvention
    {
        ArgumentNullException.ThrowIfNull(list);

        RemoveType(list, typeof(TApplicationModelConvention));
    }

    /// <summary>
    /// Removes all application model conventions of the specified type.
    /// </summary>
    /// <param name="list">The list of <see cref="IApplicationModelConvention"/>s.</param>
    /// <param name="type">The type to remove.</param>
    public static void RemoveType(this IList<IApplicationModelConvention> list, Type type)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(type);

        for (var i = list.Count - 1; i >= 0; i--)
        {
            var applicationModelConvention = list[i];
            if (applicationModelConvention.GetType() == type)
            {
                list.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Adds a <see cref="IControllerModelConvention"/> to all the controllers in the application.
    /// </summary>
    /// <param name="conventions">The list of <see cref="IApplicationModelConvention"/>
    /// in <see cref="AspNetCore.Mvc.MvcOptions"/>.</param>
    /// <param name="controllerModelConvention">The <see cref="IControllerModelConvention"/> which needs to be
    /// added.</param>
    public static void Add(
        this IList<IApplicationModelConvention> conventions,
        IControllerModelConvention controllerModelConvention)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentNullException.ThrowIfNull(controllerModelConvention);

        conventions.Add(new ControllerApplicationModelConvention(controllerModelConvention));
    }

    /// <summary>
    /// Adds a <see cref="IActionModelConvention"/> to all the actions in the application.
    /// </summary>
    /// <param name="conventions">The list of <see cref="IApplicationModelConvention"/>
    /// in <see cref="AspNetCore.Mvc.MvcOptions"/>.</param>
    /// <param name="actionModelConvention">The <see cref="IActionModelConvention"/> which needs to be
    /// added.</param>
    public static void Add(
        this IList<IApplicationModelConvention> conventions,
        IActionModelConvention actionModelConvention)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentNullException.ThrowIfNull(actionModelConvention);

        conventions.Add(new ActionApplicationModelConvention(actionModelConvention));
    }

    /// <summary>
    /// Adds a <see cref="IParameterModelConvention"/> to all the parameters in the application.
    /// </summary>
    /// <param name="conventions">The list of <see cref="IApplicationModelConvention"/>
    /// in <see cref="AspNetCore.Mvc.MvcOptions"/>.</param>
    /// <param name="parameterModelConvention">The <see cref="IParameterModelConvention"/> which needs to be
    /// added.</param>
    public static void Add(
        this IList<IApplicationModelConvention> conventions,
        IParameterModelConvention parameterModelConvention)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentNullException.ThrowIfNull(parameterModelConvention);

        conventions.Add(new ParameterApplicationModelConvention(parameterModelConvention));
    }

    /// <summary>
    /// Adds a <see cref="IParameterModelBaseConvention"/> to all properties and parameters in the application.
    /// </summary>
    /// <param name="conventions">The list of <see cref="IApplicationModelConvention"/>
    /// in <see cref="AspNetCore.Mvc.MvcOptions"/>.</param>
    /// <param name="parameterModelConvention">The <see cref="IParameterModelBaseConvention"/> which needs to be
    /// added.</param>
    public static void Add(
        this IList<IApplicationModelConvention> conventions,
        IParameterModelBaseConvention parameterModelConvention)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentNullException.ThrowIfNull(parameterModelConvention);

        conventions.Add(new ParameterBaseApplicationModelConvention(parameterModelConvention));
    }

    private sealed class ParameterApplicationModelConvention : IApplicationModelConvention
    {
        private readonly IParameterModelConvention _parameterModelConvention;

        public ParameterApplicationModelConvention(IParameterModelConvention parameterModelConvention)
        {
            _parameterModelConvention = parameterModelConvention;
        }

        /// <inheritdoc />
        public void Apply(ApplicationModel application)
        {
            ArgumentNullException.ThrowIfNull(application);

            // Create copies of collections of controllers, actions and parameters as users could modify
            // these collections from within the convention itself.
            var controllers = application.Controllers.ToArray();
            foreach (var controller in controllers)
            {
                var actions = controller.Actions.ToArray();
                foreach (var action in actions)
                {
                    var parameters = action.Parameters.ToArray();
                    foreach (var parameter in parameters)
                    {
                        _parameterModelConvention.Apply(parameter);
                    }
                }
            }
        }
    }

    private sealed class ParameterBaseApplicationModelConvention :
        IApplicationModelConvention, IParameterModelBaseConvention
    {
        private readonly IParameterModelBaseConvention _parameterBaseModelConvention;

        public ParameterBaseApplicationModelConvention(IParameterModelBaseConvention parameterModelBaseConvention)
        {
            _parameterBaseModelConvention = parameterModelBaseConvention;
        }

        /// <inheritdoc />
        public void Apply(ApplicationModel application)
        {
            ArgumentNullException.ThrowIfNull(application);
        }

        void IParameterModelBaseConvention.Apply(ParameterModelBase parameterModel)
        {
            ArgumentNullException.ThrowIfNull(parameterModel);

            _parameterBaseModelConvention.Apply(parameterModel);
        }
    }

    private sealed class ActionApplicationModelConvention : IApplicationModelConvention
    {
        private readonly IActionModelConvention _actionModelConvention;

        public ActionApplicationModelConvention(IActionModelConvention actionModelConvention)
        {
            ArgumentNullException.ThrowIfNull(actionModelConvention);

            _actionModelConvention = actionModelConvention;
        }

        /// <inheritdoc />
        public void Apply(ApplicationModel application)
        {
            ArgumentNullException.ThrowIfNull(application);

            // Create copies of collections of controllers, actions and parameters as users could modify
            // these collections from within the convention itself.
            var controllers = application.Controllers.ToArray();
            foreach (var controller in controllers)
            {
                var actions = controller.Actions.ToArray();
                foreach (var action in actions)
                {
                    _actionModelConvention.Apply(action);
                }
            }
        }
    }

    private sealed class ControllerApplicationModelConvention : IApplicationModelConvention
    {
        private readonly IControllerModelConvention _controllerModelConvention;

        public ControllerApplicationModelConvention(IControllerModelConvention controllerConvention)
        {
            ArgumentNullException.ThrowIfNull(controllerConvention);

            _controllerModelConvention = controllerConvention;
        }

        /// <inheritdoc />
        public void Apply(ApplicationModel application)
        {
            ArgumentNullException.ThrowIfNull(application);

            var controllers = application.Controllers.ToArray();
            foreach (var controller in controllers)
            {
                _controllerModelConvention.Apply(controller);
            }
        }
    }
}
