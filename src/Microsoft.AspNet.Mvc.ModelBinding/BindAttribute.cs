// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// This attribute can be used on action parameters and types, to indicate model level metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public sealed class BindAttribute : Attribute, IModelNameProvider, IModelPropertyBindingInfo
    {
        /// <summary>
        /// Comma separated set of properties which are to be excluded during model binding.
        /// </summary>
        public string Exclude { get; set; } = string.Empty;

        /// <summary>
        /// Comma separated set of properties which are to be included during model binding.
        /// </summary>
        public string Include { get; set; } = string.Empty;

        // This property is exposed for back compat reasons.
        /// <summary>
        /// Allows a user to specify a particular prefix to match during model binding.
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Represents the model name used during model binding.
        /// </summary>
        string IModelNameProvider.Name
        {
            get
            {
                return Prefix;
            }
        }

        public static bool IsPropertyAllowed(string propertyName,
                                             IReadOnlyList<string> includeProperties,
                                             IReadOnlyList<string> excludeProperties)
        {
            // We allow a property to be bound if its both in the include list AND not in the exclude list.
            // An empty include list implies all properties are allowed.
            // An empty exclude list implies no properties are disallowed.
            var includeProperty = (includeProperties == null) || 
                                   (includeProperties.Count == 0) || 
                                   includeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
            var excludeProperty = (excludeProperties != null) &&
                                  excludeProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);

            return includeProperty && !excludeProperty;
        }
    }
}
