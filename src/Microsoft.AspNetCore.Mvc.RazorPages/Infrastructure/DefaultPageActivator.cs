// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// <see cref="IPageActivatorProvider"/> that uses type activation to create Pages.
    /// </summary>
    public class DefaultPageActivator : IPageActivatorProvider
    {
        private readonly Action<PageContext, object> _disposer = Dispose;

        /// <inheritdoc />
        public virtual Func<PageContext, object> CreateActivator(CompiledPageActionDescriptor actionDescriptor)
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

        public virtual Action<PageContext, object> CreateReleaser(CompiledPageActionDescriptor actionDescriptor)
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

        private static Func<PageContext, object> CreatePageFactory(Type pageTypeInfo)
        {
            var parameter = Expression.Parameter(typeof(PageContext), "pageContext");

            // new Page();
            var newExpression = Expression.New(pageTypeInfo);

            // () => new Page();
            var pageFactory = Expression
                .Lambda<Func<PageContext, object>>(newExpression, parameter)
                .Compile();
            return pageFactory;
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

        private static void NullDisposer(PageContext context, object page)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            // No-op
        }
    }
}
