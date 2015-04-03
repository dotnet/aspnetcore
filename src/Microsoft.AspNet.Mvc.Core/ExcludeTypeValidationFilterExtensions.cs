// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Extensions for <see cref="IList{IExcludeTypeValidationFilter}"/>.
    /// </summary>
    public static class ExcludeTypeValidationFilterExtensions
    {
        /// <summary>
        /// Adds a descriptor to the specified <paramref name="excludeTypeValidationFilters" /> that excludes the properties of 
        /// the <see cref="Type"/> specified and its derived types from validaton.
        /// </summary>
        /// <param name="excludeTypeValidationFilters">A list of <see cref="IExcludeTypeValidationFilter"/> which are used to
        /// get a collection of exclude filters to be applied for filtering model properties during validation.
        /// </param>
        /// <param name="type"><see cref="Type"/> which should be excluded from validation.</param>
        public static void Add(this IList<IExcludeTypeValidationFilter> excludeTypeValidationFilters, Type type)
        {
            var typeBasedExcludeFilter = new DefaultTypeBasedExcludeFilter(type);
            excludeTypeValidationFilters.Add(typeBasedExcludeFilter);
        }

        /// <summary>
        /// Adds a descriptor to the specified <paramref name="excludeTypeValidationFilters" /> that excludes the properties of 
        /// the type specified and its derived types from validaton.
        /// </summary>
        /// <param name="excludeTypeValidationFilters">A list of <see cref="IExcludeTypeValidationFilter"/> which are used to
        /// get a collection of exclude filters to be applied for filtering model properties during validation.
        /// </param>
        /// <param name="typeFullName">Full name of the type which should be excluded from validation.</param>
        public static void Add(this IList<IExcludeTypeValidationFilter> excludeTypeValidationFilters, string typeFullName)
        {
            var filter = new DefaultTypeNameBasedExcludeFilter(typeFullName);
            excludeTypeValidationFilters.Add(filter);
        }
    }
}