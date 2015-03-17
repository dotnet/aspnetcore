// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.AspNet.Mvc.OptionDescriptors;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Extensions for <see cref="MvcOptions.ValidationExcludeFilters"/>.
    /// </summary>
    public static class ValidationExcludeFiltersExtensions
    {
        /// <summary>
        /// Adds a descriptor to the specified <paramref name="descriptorCollection" /> that excludes the properties of 
        /// the <see cref="Type"/> specified and its derived types from validaton.
        /// </summary>
        /// <param name="descriptorCollection">A list of <see cref="ExcludeValidationDescriptor"/> which are used to
        /// get a collection of exclude filters to be applied for filtering model properties during validation.
        /// </param>
        /// <param name="type"><see cref="Type"/> which should be excluded from validation.</param>
        public static void Add(this IList<ExcludeValidationDescriptor> descriptorCollection, Type type)
        {
            var typeBasedExcludeFilter = new DefaultTypeBasedExcludeFilter(type);
            descriptorCollection.Add(new ExcludeValidationDescriptor(typeBasedExcludeFilter));
        }

        /// <summary>
        /// Adds a descriptor to the specified <paramref name="descriptorCollection" /> that excludes the properties of 
        /// the type specified and its derived types from validaton.
        /// </summary>
        /// <param name="descriptorCollection">A list of <see cref="ExcludeValidationDescriptor"/> which are used to
        /// get a collection of exclude filters to be applied for filtering model properties during validation.
        /// </param>
        /// <param name="typeFullName">Full name of the type which should be excluded from validation.</param>
        public static void Add(this IList<ExcludeValidationDescriptor> descriptorCollection, string typeFullName)
        {
            var filter = new DefaultTypeNameBasedExcludeFilter(typeFullName);
            descriptorCollection.Add(new ExcludeValidationDescriptor(filter));
        }

        /// <summary>
        /// Adds a descriptor to the specified <paramref name="descriptorCollection" /> that excludes the properties of 
        /// the type specified and its derived types from validaton.
        /// </summary>
        /// <param name="descriptorCollection">A list of <see cref="ExcludeValidationDescriptor"/> which are used to
        /// get a collection of exclude filters to be applied for filtering model properties during validation.
        /// </param>
        /// <param name="filter"><see cref="IExcludeTypeValidationFilter"/> which should be excluded from validation.
        /// </param>
        public static void Add(this IList<ExcludeValidationDescriptor> descriptorCollection, 
                               IExcludeTypeValidationFilter filter)
        {
            descriptorCollection.Add(new ExcludeValidationDescriptor(filter));
        }
    }
}