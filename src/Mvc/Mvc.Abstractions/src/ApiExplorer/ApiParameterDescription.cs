// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
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

        /// <summary>
        /// Gets or sets the parameter descriptor.
        /// </summary>
        public ParameterDescriptor ParameterDescriptor { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if the parameter is required.
        /// </summary>
        /// <remarks>
        /// A parameter is considered required if
        /// <list type="bullet">
        /// <item>it's bound from the request body (<see cref="BindingSource.Body"/>).</item>
        /// <item>it's a required route value.</item>
        /// <item>it has annotations (e.g. BindRequiredAttribute) that indicate it's required.</item>
        /// </list>
        /// </remarks>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets the default value for a parameter.
        /// </summary>
        public object DefaultValue { get; set; }
    }
}