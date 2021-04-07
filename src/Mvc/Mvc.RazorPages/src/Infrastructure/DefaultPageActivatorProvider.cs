// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// <see cref="IPageActivatorProvider"/> that uses type activation to create Pages.
    /// </summary>
    internal class DefaultPageActivatorProvider : IPageActivatorProvider
    {
        private readonly Action<PageContext, ViewContext, object> _disposer = Dispose;
        private readonly Func<PageContext, ViewContext, object, ValueTask> _asyncDisposer = AsyncDispose;
        private readonly Func<PageContext, ViewContext, object, ValueTask> _syncAsyncDisposer = SyncAsyncDispose;

        /// <inheritdoc />
        public Func<PageContext, ViewContext, object> CreateActivator(CompiledPageActionDescriptor actionDescriptor)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            var pageTypeInfo = actionDescriptor.PageTypeInfo?.AsType();
            if (pageTypeInfo == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(actionDescriptor.PageTypeInfo),
                    nameof(actionDescriptor)),
                    nameof(actionDescriptor));
            }

            return CreatePageFactory(pageTypeInfo);
        }

        public Action<PageContext, ViewContext, object> CreateReleaser(CompiledPageActionDescriptor actionDescriptor)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            if (typeof(IDisposable).GetTypeInfo().IsAssignableFrom(actionDescriptor.PageTypeInfo))
            {
                return _disposer;
            }

            return null;
        }

        public Func<PageContext, ViewContext, object, ValueTask> CreateAsyncReleaser(CompiledPageActionDescriptor actionDescriptor)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            if (typeof(IAsyncDisposable).GetTypeInfo().IsAssignableFrom(actionDescriptor.PageTypeInfo))
            {
                return _asyncDisposer;
            }

            if (typeof(IDisposable).GetTypeInfo().IsAssignableFrom(actionDescriptor.PageTypeInfo))
            {
                return _syncAsyncDisposer;
            }

            return null;
        }

        private static Func<PageContext, ViewContext, object> CreatePageFactory(Type pageTypeInfo)
        {
            var parameter1 = Expression.Parameter(typeof(PageContext), "pageContext");
            var parameter2 = Expression.Parameter(typeof(ViewContext), "viewContext");

            // new Page();
            var newExpression = Expression.New(pageTypeInfo);

            // () => new Page();
            var pageFactory = Expression
                .Lambda<Func<PageContext, ViewContext, object>>(newExpression, parameter1, parameter2)
                .Compile();
            return pageFactory;
        }

        private static void Dispose(PageContext context, ViewContext viewContext, object page)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            ((IDisposable)page).Dispose();
        }

        private static ValueTask SyncAsyncDispose(PageContext context, ViewContext viewContext, object page)
        {
            Dispose(context, viewContext, page);
            return default;
        }

        private static ValueTask AsyncDispose(PageContext context, ViewContext viewContext, object page)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            return ((IAsyncDisposable)page).DisposeAsync();
        }
    }
}
