// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Descriptor for an <see cref="IFilter"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="FilterDescriptor"/> describes an <see cref="IFilter"/> with an order and scope. 
    /// 
    /// Order and scope control the execution order of filters. Filters with a higher value of Order execute
    /// later in the pipeline. 
    /// 
    /// When filters have the same Order, the Scope value is used to determine the order of execution. Filters
    /// with a higher value of Scope execute later in the pipeline. See <see cref="FilterScope"/> for commonly
    /// used scopes.
    /// 
    /// For <see cref="IExceptionFilter"/> implementions, the filter runs only after an exception has occurred,
    /// and so the observed order of execution will be opposite that of other filters.
    /// </remarks>
    public class FilterDescriptor
    {
        /// <summary>
        /// Creates a new <see cref="FilterDescriptor"/>.
        /// </summary>
        /// <param name="filter">The <see cref="IFilter"/>.</param>
        /// <param name="filterScope">The filter scope.</param>
        /// <remarks>
        /// If the <paramref name="filter"/> implements <see cref="IOrderedFilter"/>, then the value of 
        /// <see cref="Order"/> will be taken from <see cref="IOrderedFilter.Order"/>. Otherwise the value
        /// of <see cref="Order"/> will default to <c>0</c>.
        /// </remarks>
        public FilterDescriptor([NotNull] IFilter filter, int filterScope)
        {
            Filter = filter;
            Scope = filterScope;

            var orderedFilter = Filter as IOrderedFilter;

            if (orderedFilter != null)
            {
                Order = orderedFilter.Order;
            }
        }

        /// <summary>
        /// The <see cref="IFilter"/> instance.
        /// </summary>
        public IFilter Filter { get; private set; }

        /// <summary>
        /// The filter order.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// The filter scope.
        /// </summary>
        public int Scope { get; private set; }
    }
}
