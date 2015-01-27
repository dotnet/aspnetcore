// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// <see cref="IControllerActivator"/> that uses type activation to create controllers.
    /// </summary>
    public class DefaultControllerActivator : IControllerActivator
    {
        private static readonly Func<Type, ObjectFactory> _createControllerFactory =
            type => ActivatorUtilities.CreateFactory(type, Type.EmptyTypes);

        private readonly ConcurrentDictionary<Type, ObjectFactory> _controllerFactories =
            new ConcurrentDictionary<Type, ObjectFactory>();

        /// <inheritdoc />
        public object Create([NotNull] ActionContext actionContext, [NotNull] Type controllerType)
        {
            var factory = _controllerFactories.GetOrAdd(controllerType, _createControllerFactory);
            return factory(actionContext.HttpContext.RequestServices, arguments: null);
        }
    }
}
