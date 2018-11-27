// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// This attribute can be used on action parameters and types, to indicate model level metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class BindAttribute : Attribute, IModelNameProvider, IPropertyFilterProvider
    {
        private static readonly Func<ModelMetadata, bool> _default = (m) => true;

        private Func<ModelMetadata, bool> _propertyFilter;

        /// <summary>
        /// Creates a new instance of <see cref="BindAttribute"/>.
        /// </summary>
        /// <param name="include">Names of parameters to include in binding.</param>
        public BindAttribute(params string[] include)
        {
            var items = new List<string>();
            foreach (var item in include)
            {
                items.AddRange(SplitString(item));
            }

            Include = items.ToArray();
        }

        /// <summary>
        /// Gets the names of properties to include in model binding.
        /// </summary>
        public string[] Include { get; }

        /// <summary>
        /// Allows a user to specify a particular prefix to match during model binding.
        /// </summary>
        // This property is exposed for back compat reasons.
        public string Prefix { get; set; }

        /// <summary>
        /// Represents the model name used during model binding.
        /// </summary>
        string IModelNameProvider.Name => Prefix;

        /// <inheritdoc />
        public Func<ModelMetadata, bool> PropertyFilter
        {
            get
            {
                if (Include != null && Include.Length > 0)
                {
                    if (_propertyFilter == null)
                    {
                        _propertyFilter = (m) => Include.Contains(m.PropertyName, StringComparer.Ordinal);
                    }

                    return _propertyFilter;
                }
                else
                {
                    return _default;
                }
            }
        }

        private static IEnumerable<string> SplitString(string original)
        {
            if (string.IsNullOrEmpty(original))
            {
                return Array.Empty<string>();
            }

            var split = original.Split(',').Select(piece => piece.Trim()).Where(piece => !string.IsNullOrEmpty(piece));

            return split;
        }
    }
}
