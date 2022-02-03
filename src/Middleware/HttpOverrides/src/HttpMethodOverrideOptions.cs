// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpOverrides;

namespace Microsoft.AspNetCore.Builder;

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
