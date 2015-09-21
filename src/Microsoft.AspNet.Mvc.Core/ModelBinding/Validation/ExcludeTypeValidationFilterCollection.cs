// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class ExcludeTypeValidationFilterCollection : Collection<IExcludeTypeValidationFilter>
    {
        /// <summary>
        /// Adds an <see cref="IExcludeTypeValidationFilter"/> that excludes the properties of 
        /// the <see cref="Type"/> specified and its derived types from validaton.
        /// </summary>
        /// <param name="type"><see cref="Type"/> which should be excluded from validation.</param>
        public void Add([NotNull] Type type)
        {
            var typeBasedExcludeFilter = new DefaultTypeBasedExcludeFilter(type);
            Add(typeBasedExcludeFilter);
        }

        /// <summary>
        /// Adds an <see cref="IExcludeTypeValidationFilter"/> that excludes the properties of 
        /// the <see cref="Type"/> specified and its derived types from validaton.
        /// </summary>
        /// <param name="typeFullName">Full name of the type which should be excluded from validation.</param>
        public void Add([NotNull] string typeFullName)
        {
            var filter = new DefaultTypeNameBasedExcludeFilter(typeFullName);
            Add(filter);
        }
    }
}
