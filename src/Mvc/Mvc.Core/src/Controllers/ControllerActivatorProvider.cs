// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    /// <summary>
    /// Provides methods to create an MVC controller.
    /// </summary>
    public class ControllerActivatorProvider : IControllerActivatorProvider
    {
        private static readonly Action<ControllerContext, object> _dispose = Dispose;
        private static readonly Func<ControllerContext, object, ValueTask> _disposeAsync = DisposeAsync;
        private static readonly Func<ControllerContext, object, ValueTask> _syncDisposeAsync = SyncDisposeAsync;
        private readonly Func<ControllerContext, object> _controllerActivatorCreate;
        private readonly Action<ControllerContext, object> _controllerActivatorRelease;
        private readonly Func<ControllerContext, object, ValueTask> _controllerActivatorReleaseAsync;

        /// <summary>
        /// Initializes a new instance of <see cref="ControllerActivatorProvider"/>.
        /// </summary>
        /// <param name="controllerActivator">A <see cref="IControllerActivator"/> which is delegated to when not the default implementation.</param>
        public ControllerActivatorProvider(IControllerActivator controllerActivator)
        {
            if (controllerActivator == null)
            {
                throw new ArgumentNullException(nameof(controllerActivator));
            }

            // Compat: Delegate to controllerActivator if it's not the default implementation.
            if (controllerActivator.GetType() != typeof(DefaultControllerActivator))
            {
                _controllerActivatorCreate = controllerActivator.Create;
                _controllerActivatorRelease = controllerActivator.Release;
                _controllerActivatorReleaseAsync = controllerActivator.ReleaseAsync;
            }
        }

        /// <inheritdoc/>
        public Func<ControllerContext, object> CreateActivator(ControllerActionDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var controllerType = descriptor.ControllerTypeInfo?.AsType();
            if (controllerType == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(descriptor.ControllerTypeInfo),
                    nameof(descriptor)),
                    nameof(descriptor));
            }

            if (_controllerActivatorCreate != null)
            {
                return _controllerActivatorCreate;
            }

            var typeActivator = ActivatorUtilities.CreateFactory(controllerType, Type.EmptyTypes);
            return controllerContext => typeActivator(controllerContext.HttpContext.RequestServices, arguments: null);
        }

        /// <inheritdoc/>
        public Action<ControllerContext, object> CreateReleaser(ControllerActionDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (_controllerActivatorRelease != null)
            {
                return _controllerActivatorRelease;
            }

            if (typeof(IDisposable).GetTypeInfo().IsAssignableFrom(descriptor.ControllerTypeInfo))
            {
                return _dispose;
            }

            return null;
        }

        /// <inheritdoc/>
        public Func<ControllerContext, object, ValueTask> CreateAsyncReleaser(ControllerActionDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (_controllerActivatorReleaseAsync != null)
            {
                return _controllerActivatorReleaseAsync;
            }

            if (typeof(IAsyncDisposable).GetTypeInfo().IsAssignableFrom(descriptor.ControllerTypeInfo))
            {
                return _disposeAsync;
            }

            if (typeof(IDisposable).GetTypeInfo().IsAssignableFrom(descriptor.ControllerTypeInfo))
            {
                return _syncDisposeAsync;
            }

            return null;
        }

        private static void Dispose(ControllerContext context, object controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            ((IDisposable)controller).Dispose();
        }

        private static ValueTask DisposeAsync(ControllerContext context, object controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            return ((IAsyncDisposable)controller).DisposeAsync();
        }

        private static ValueTask SyncDisposeAsync(ControllerContext context, object controller)
        {
            Dispose(context, controller);
            return default;
        }
    }
}
