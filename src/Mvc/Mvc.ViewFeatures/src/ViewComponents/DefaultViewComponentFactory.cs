// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    /// <summary>
    /// Default implementation for <see cref="IViewComponentFactory"/>.
    /// </summary>
    public class DefaultViewComponentFactory : IViewComponentFactory
    {
        private readonly IViewComponentActivator _activator;
        private readonly Func<Type, PropertyActivator<ViewComponentContext>[]> _getPropertiesToActivate;
        private readonly ConcurrentDictionary<Type, PropertyActivator<ViewComponentContext>[]> _injectActions;

        /// <summary>
        /// Creates a new instance of <see cref="DefaultViewComponentFactory"/>
        /// </summary>
        /// <param name="activator">
        /// The <see cref="IViewComponentActivator"/> used to create new view component instances.
        /// </param>
        public DefaultViewComponentFactory(IViewComponentActivator activator)
        {
            if (activator == null)
            {
                throw new ArgumentNullException(nameof(activator));
            }

            _activator = activator;

            _getPropertiesToActivate = type => PropertyActivator<ViewComponentContext>.GetPropertiesToActivate(
                type,
                typeof(ViewComponentContextAttribute),
                CreateActivateInfo);

            _injectActions = new ConcurrentDictionary<Type, PropertyActivator<ViewComponentContext>[]>();
        }

        /// <inheritdoc />
        public object CreateViewComponent(ViewComponentContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var component = _activator.Create(context);

            InjectProperties(context, component);

            return component;
        }

        private void InjectProperties(ViewComponentContext context, object viewComponent)
        {
            var propertiesToActivate = _injectActions.GetOrAdd(
                viewComponent.GetType(),
                _getPropertiesToActivate);

            for (var i = 0; i < propertiesToActivate.Length; i++)
            {
                var activateInfo = propertiesToActivate[i];
                activateInfo.Activate(viewComponent, context);
            }
        }

        private static PropertyActivator<ViewComponentContext> CreateActivateInfo(PropertyInfo property)
        {
            return new PropertyActivator<ViewComponentContext>(property, context => context);
        }

        /// <inheritdoc />
        public void ReleaseViewComponent(ViewComponentContext context, object component)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            _activator.Release(context, component);
        }
    }
}
