// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// <see cref="FromHeaderAttribute"/> can be placed on an action parameter or model property to indicate
    /// that model binding should use a header value as the data source.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FromHeaderAttribute : Attribute, IHeaderBinderMetadata, IModelNameProvider
    {
        /// <inheritdoc />
        public string Name { get; set; }
    }
}
