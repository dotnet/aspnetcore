// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Api;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Specifies that a parameter or property should be bound using route-data from the current request.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FromRouteAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider, IFromRouteMetadata
    {
        /// <inheritdoc />
        public BindingSource BindingSource => BindingSource.Path;

        /// <summary>
        /// The <see cref="HttpRequest.RouteValues"/> name.
        /// </summary>
        public string Name { get; set; }
    }
}
