// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// <see cref="IPageActivatorProvider"/> that uses type activation to create Razor Page instances.
    /// </summary>
    internal class DefaultPageModelActivatorProvider : IPageModelActivatorProvider
    {
        private readonly Action<PageContext, object> _disposer = Dispose;
        private readonly Func<PageContext, object, ValueTask> _asyncDisposer = DisposeAsync;
        private readonly Func<PageContext, object, ValueTask> _syncAsyncDisposer = SyncDisposeAsync;

        /// <inheritdoc />
        public virtual Func<PageContext, object> CreateActivator(CompiledPageActionDescriptor actionDescriptor)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            var modelTypeInfo = actionDescriptor.ModelTypeInfo?.AsType();
            if (modelTypeInfo == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(actionDescriptor.ModelTypeInfo),
                    nameof(actionDescriptor)),
                    nameof(actionDescriptor));
            }

            var factory = ActivatorUtilities.CreateFactory(modelTypeInfo, Type.EmptyTypes);
            return (context) => factory(context.HttpContext.RequestServices, Array.Empty<object>());
        }

        public virtual Action<PageContext, object> CreateReleaser(CompiledPageActionDescriptor actionDescriptor)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            if (typeof(IDisposable).GetTypeInfo().IsAssignableFrom(actionDescriptor.ModelTypeInfo))
            {
                return _disposer;
            }

            return null;
        }

        public virtual Func<PageContext, object, ValueTask> CreateAsyncReleaser(CompiledPageActionDescriptor actionDescriptor)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            if (typeof(IAsyncDisposable).GetTypeInfo().IsAssignableFrom(actionDescriptor.ModelTypeInfo))
            {
                return _asyncDisposer;
            }

            if (typeof(IDisposable).GetTypeInfo().IsAssignableFrom(actionDescriptor.ModelTypeInfo))
            {
                return _syncAsyncDisposer;
            }

            return null;
        }

        private static void Dispose(PageContext context, object page)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            ((IDisposable)page).Dispose();
        }

        private static ValueTask DisposeAsync(PageContext context, object page)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            return ((IAsyncDisposable)page).DisposeAsync();
        }

        private static ValueTask SyncDisposeAsync(PageContext context, object page)
        {
            Dispose(context, page);
            return default;
        }
    }
}
