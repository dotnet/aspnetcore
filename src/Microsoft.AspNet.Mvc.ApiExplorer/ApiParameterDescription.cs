// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.ApiExplorer
{
    /// <summary>
    /// A metadata description of an input to an API.
    /// </summary>
    public class ApiParameterDescription
    {
        /// <summary>
        /// Gets or sets the <see cref="ModelMetadata"/>.
        /// </summary>
        public ModelMetadata ModelMetadata { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ApiParameterRouteInfo"/>.
        /// </summary>
        public ApiParameterRouteInfo RouteInfo { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="BindingSource"/>.
        /// </summary>
        public BindingSource Source { get; set; }

        /// <summary>
        /// Gets or sets the parameter type.
        /// </summary>
        public Type Type { get; set; }
    }
}