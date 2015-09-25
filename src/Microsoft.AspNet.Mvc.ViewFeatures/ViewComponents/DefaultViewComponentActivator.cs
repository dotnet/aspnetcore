// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    /// <summary>
    /// A default implementation of <see cref="IViewComponentActivator"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="DefaultViewComponentActivator"/> can provide the current instance of
    /// <see cref="ViewComponentContext"/> to a public property of a view component marked
    /// with <see cref="ViewComponentContextAttribute"/>. 
    /// </remarks>
    public class DefaultViewComponentActivator : IViewComponentActivator
    {
        private readonly Func<Type, PropertyActivator<ViewComponentContext>[]> _getPropertiesToActivate;
        private readonly ConcurrentDictionary<Type, PropertyActivator<ViewComponentContext>[]> _injectActions;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultViewComponentActivator"/> class.
        /// </summary>
        public DefaultViewComponentActivator()
        {
            _injectActions = new ConcurrentDictionary<Type, PropertyActivator<ViewComponentContext>[]>();
            _getPropertiesToActivate = type =>
                PropertyActivator<ViewComponentContext>.GetPropertiesToActivate(
                    type,
                    typeof(ViewComponentContextAttribute),
                    CreateActivateInfo);
        }

        /// <inheritdoc />
        public virtual void Activate(object viewComponent, ViewComponentContext context)
        {
            if (viewComponent == null)
            {
                throw new ArgumentNullException(nameof(viewComponent));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var propertiesToActivate = _injectActions.GetOrAdd(
                viewComponent.GetType(),
                _getPropertiesToActivate);

            for (var i = 0; i < propertiesToActivate.Length; i++)
            {
                var activateInfo = propertiesToActivate[i];
                activateInfo.Activate(viewComponent, context);
            }
        }

        private PropertyActivator<ViewComponentContext> CreateActivateInfo(PropertyInfo property)
        {
            return new PropertyActivator<ViewComponentContext>(property, context => context);
        }
    }
}