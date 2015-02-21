// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides an implementation of <see cref="IExcludeTypeValidationFilter"/> which can filter
    /// based on a type.
    /// </summary>
    public class DefaultTypeBasedExcludeFilter : IExcludeTypeValidationFilter
    {
        /// <summary>
        /// Creates a new instance of <see cref="DefaultTypeBasedExcludeFilter"/>.
        /// </summary>
        /// <param name="type">The type which needs to be excluded.</param>
        public DefaultTypeBasedExcludeFilter([NotNull] Type type)
        {
            ExcludedType = type;
        }

        /// <summary>
        /// Gets the type which is excluded from validation.
        /// </summary>
        public Type ExcludedType { get; }

        /// <inheritdoc />
        public bool IsTypeExcluded([NotNull] Type propertyType)
        {
            return ExcludedType.IsAssignableFrom(propertyType);
        }
    }
}
