// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides methods for configuring a Razor components based application.
/// </summary>
public static class RazorComponentsRazorComponentBuilderExtensions
{
    /// <summary>
    /// Registers a persistent service with the specified render mode in the Razor components builder.
    /// </summary>
    /// <typeparam name="TPersistentService">The service to be registered for state management.</typeparam>
    /// <param name="builder">The <see cref="IRazorComponentsBuilder"/>.</param>
    /// <param name="renderMode">The <see cref="IComponentRenderMode"/> that determines to which render modes the state should be persisted.</param>
    /// <returns>The <see cref="IRazorComponentsBuilder"/>.</returns>
    public static IRazorComponentsBuilder RegisterPersistentService<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TPersistentService>(
        this IRazorComponentsBuilder builder,
        IComponentRenderMode renderMode)
    {
        ArgumentNullException.ThrowIfNull(builder);

        RegisterPersistentComponentStateServiceCollectionExtensions.AddPersistentServiceRegistration<TPersistentService>(
            builder.Services,
            renderMode);

        return builder;
    }
}
