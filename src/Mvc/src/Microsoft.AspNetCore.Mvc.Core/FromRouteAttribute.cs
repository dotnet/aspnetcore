// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Specifies that a parameter or property should be bound using route-data from the current request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FromRouteAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider
    {
        /// <inheritdoc />
        public BindingSource BindingSource => BindingSource.Path;

        /// <inheritdoc />
        public string Name { get; set; }
    }
}
