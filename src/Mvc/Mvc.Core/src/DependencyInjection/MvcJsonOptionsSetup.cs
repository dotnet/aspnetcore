// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.DependencyInjection;

/// <summary>
/// Sets up JSON options for MVC.
/// </summary>
internal sealed class MvcJsonOptionsSetup : IConfigureOptions<JsonOptions>
{
    /// <inheritdoc/>
    public void Configure(JsonOptions options)
    {
        options.JsonSerializerOptions.Converters.Add(new ValidationProblemDetailsJsonConverter());
    }
}