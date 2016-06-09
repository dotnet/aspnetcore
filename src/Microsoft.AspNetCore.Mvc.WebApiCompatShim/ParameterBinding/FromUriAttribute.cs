// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebApiShimResources = Microsoft.AspNetCore.Mvc.WebApiCompatShim.Resources;

namespace System.Web.Http
{
    /// <summary>
    /// An attribute that specifies that the value can be bound from the query string or route data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class FromUriAttribute :
        Attribute,
        IOptionalBinderMetadata,
        IBindingSourceMetadata,
        IModelNameProvider
    {
        private static readonly BindingSource FromUriSource = CompositeBindingSource.Create(
            new BindingSource[] { BindingSource.Path, BindingSource.Query },
            WebApiShimResources.BindingSource_URL);

        /// <inheritdoc />
        public BindingSource BindingSource { get { return FromUriSource; } }

        /// <inheritdoc />
        public bool IsOptional { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }
    }
}