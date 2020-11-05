// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.HttpOverrides;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Options for configuring <see cref="HttpMethodOverrideMiddleware"/>
    /// </summary>
    public class HttpMethodOverrideOptions
    {
        /// <summary>
        /// Denotes the form element that contains the name of the resulting method type.
        /// If not set the X-Http-Method-Override header will be used.
        /// </summary>
        public string? FormFieldName { get; set; }
    }
}
