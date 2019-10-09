// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// Descriptor for an <see cref="IFilterMetadata"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="FilterDescriptor"/> describes an <see cref="IFilterMetadata"/> with an order and scope.
    ///
    /// Order and scope control the execution order of filters. Filters with a higher value of Order execute
    /// later in the pipeline.
    ///
    /// When filters have the same Order, the Scope value is used to determine the order of execution. Filters
    /// with a higher value of Scope execute later in the pipeline. See <c>Microsoft.AspNetCore.Mvc.FilterScope</c>
    /// for commonly used scopes.
    ///
    /// For <see cref="IExceptionFilter"/> implementations, the filter runs only after an exception has occurred,
    /// and so the observed order of execution will be opposite that of other filters.
    /// </remarks>
    [DebuggerDisplay("Filter = {Filter.ToString(),nq}, Order = {Order}")]
    public class FilterDescriptor
    {
        /// <summary>
        /// Creates a new <see cref="FilterDescriptor"/>.
        /// </summary>
        /// <param name="filter">The <see cref="IFilterMetadata"/>.</param>
        /// <param name="filterScope">The filter scope.</param>
        /// <remarks>
        /// If the <paramref name="filter"/> implements <see cref="IOrderedFilter"/>, then the value of
        /// <see cref="Order"/> will be taken from <see cref="IOrderedFilter.Order"/>. Otherwise the value
        /// of <see cref="Order"/> will default to <c>0</c>.
        /// </remarks>
        public FilterDescriptor(IFilterMetadata filter, int filterScope)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            Filter = filter;
            Scope = filterScope;


            if (Filter is IOrderedFilter orderedFilter)
            {
                Order = orderedFilter.Order;
            }
        }

        /// <summary>
        /// The <see cref="IFilterMetadata"/> instance.
        /// </summary>
        public IFilterMetadata Filter { get; }

        /// <summary>
        /// The filter order.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// The filter scope.
        /// </summary>
        public int Scope { get; }
    }
}
