// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logging representation of the state of a <see cref="FilterDescriptor"/>. Logged as a substructure of
    /// <see cref="ActionDescriptorValues"/>.
    /// </summary>
    public class FilterDescriptorValues : LoggerStructureBase
    {
        public FilterDescriptorValues([NotNull] FilterDescriptor inner)
        {
            Filter = new FilterValues(inner.Filter);
            Order = inner.Order;
            Scope = inner.Scope;
        }

        /// <summary>
        /// The <see cref="IFilter"/> instance of the filter descriptor as <see cref="FilterValues"/>.
        /// See <see cref="FilterDescriptor.Filter"/>.
        /// </summary>
        public FilterValues Filter { get; }

        /// <summary>
        /// The filter order. See <see cref="FilterDescriptor.Order"/>.
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// The filter scope. See <see cref="FilterDescriptor.Scope"/>.
        /// </summary>
        public int Scope { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}