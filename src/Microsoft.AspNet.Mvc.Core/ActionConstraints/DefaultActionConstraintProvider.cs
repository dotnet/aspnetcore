// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A default implementation of <see cref="INestedProvider{ActionConstraintProviderContext}"/>.
    /// </summary>
    /// <remarks>
    /// This provider is able to provide an <see cref="IActionConstraint"/> instance when the
    /// <see cref="IActionConstraintMetadata"/> implements <see cref="IActionConstraint"/> or
    /// <see cref="IActionConstraintFactory"/>/
    /// </remarks>
    public class DefaultActionConstraintProvider : INestedProvider<ActionConstraintProviderContext>
    {
        /// <inheritdoc />
        public int Order
        {
            get { return DefaultOrder.DefaultFrameworkSortOrder; }
        }

        /// <inheritdoc />
        public void Invoke([NotNull] ActionConstraintProviderContext context, [NotNull] Action callNext)
        {
            foreach (var item in context.Results)
            {
                ProvideConstraint(item, context.HttpContext.RequestServices);
            }

            callNext();
        }

        private void ProvideConstraint(ActionConstraintItem item, IServiceProvider services)
        {
            // Don't overwrite anything that was done by a previous provider.
            if (item.Constraint != null)
            {
                return;
            }

            var constraint = item.Metadata as IActionConstraint;
            if (constraint != null)
            {
                item.Constraint = constraint;
                return;
            }

            var factory = item.Metadata as IActionConstraintFactory;
            if (factory != null)
            {
                item.Constraint = factory.CreateInstance(services);
                return;
            }
        }
    }
}