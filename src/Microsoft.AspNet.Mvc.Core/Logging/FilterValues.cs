// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    /// <summary>
    /// Logging representation of an <see cref="IFilter"/>. Logged as a component of 
    /// <see cref="FilterDescriptorValues"/>, and as a substructure of <see cref="ControllerModelValues"/> 
    /// and <see cref="ActionModelValues"/>.
    /// </summary>
    public class FilterValues : LoggerStructureBase
    {
        public FilterValues(IFilter inner)
        {
            FilterMetadataType = inner.GetType();
            if (inner is IFilterFactory)
            {
                IsFactory = true;
                if (inner is ServiceFilterAttribute)
                {
                    FilterType = ((ServiceFilterAttribute)inner).ServiceType;
                }
                else if (inner is TypeFilterAttribute)
                {
                    FilterType = ((TypeFilterAttribute)inner).ImplementationType;
                }
            }
            if (FilterType != null)
            {
                FilterInterfaces = FilterType.GetInterfaces().ToList();
            }
            else
            {
                FilterInterfaces = FilterMetadataType.GetInterfaces().ToList();
            }
        }

        /// <summary>
        /// Whether or not the instance of <see cref="IFilter"/> is an <see cref="IFilterFactory"/>.
        /// </summary>
        public bool IsFactory { get; }

        /// <summary>
        /// The metadata type of the <see cref="IFilter"/>.
        /// </summary>
        public Type FilterMetadataType { get; }

        /// <summary>
        /// The inner <see cref="Type"/> of the <see cref="IFilter"/> if it is a <see cref="ServiceFilterAttribute"/>
        /// or <see cref="TypeFilterAttribute"/>.
        /// </summary>
        public Type FilterType { get; }

        /// <summary>
        /// A list of interfaces the <see cref="IFilter"/> implements.
        /// </summary>
        public List<Type> FilterInterfaces { get; }

        public override string Format()
        {
            return LogFormatter.FormatStructure(this);
        }
    }
}