// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms.ClientValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering client-side validation services.
/// </summary>
public static class ClientValidationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default <see cref="IClientValidationService"/> implementation,
    /// which generates <c>data-val-*</c> HTML attributes from <c>DataAnnotations</c> validation attributes.
    /// </summary>
    public static IServiceCollection AddClientValidation(this IServiceCollection services)
    {
        services.TryAddSingleton<IClientValidationService, DefaultClientValidationService>();
        return services;
    }
}
