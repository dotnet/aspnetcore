// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Specifies that a parameter or property should be bound using the request query string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FromQueryAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider
    {
        /// <summary>
        /// Creates a new <see cref="FromQueryAttribute"/>.
        /// </summary>
        public FromQueryAttribute() { }

        /// <summary>
        /// Creates a new <see cref="FromQueryAttribute"/> while specifying
        /// a target paramter name.
        /// </summary>
        /// <param name="name">The name of the query parameter. May not be null</param>
        public FromQueryAttribute(string name)
        {
            if(name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        /// <inheritdoc />
        public BindingSource BindingSource => BindingSource.Query;

        /// <inheritdoc />
        public string Name { get; set; }
    }
}
