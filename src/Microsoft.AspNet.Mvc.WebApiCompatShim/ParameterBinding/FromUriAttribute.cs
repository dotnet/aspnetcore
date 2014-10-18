// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace System.Web.Http
{
    /// <summary>
    /// An attribute that specifies that the value can be bound from the query string or route data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class FromUriAttribute : 
        Attribute, 
        IQueryValueProviderMetadata, 
        IRouteDataValueProviderMetadata, 
        IModelNameProvider
    {
        /// <inheritdoc />
        public string Name { get; set; }
    }
}