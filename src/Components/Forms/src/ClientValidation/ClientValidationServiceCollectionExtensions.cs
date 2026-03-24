// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Extension methods for configuring client-side validation services.
/// </summary>
public static class ClientValidationServiceCollectionExtensions
{
    /// <summary>
    /// Adds the default client-side validation services to the service collection.
    /// This enables Blazor input components to emit <c>data-val-*</c> HTML attributes
    /// for client-side validation when used with the <c>ClientSideValidator</c> component.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.AddRazorComponents();
    /// builder.Services.AddClientSideValidation();
    /// </code>
    /// </example>
    public static IServiceCollection AddClientSideValidation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions();

        // Register built-in adapters for standard validation attributes.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IConfigureOptions<ClientValidationAdapterRegistry>, BuiltInAdapterRegistration>());

        services.TryAddScoped<IClientValidationService>(sp =>
        {
            var registry = sp.GetRequiredService<IOptions<ClientValidationAdapterRegistry>>().Value;
            var validationOptions = sp.GetRequiredService<IOptions<ValidationOptions>>();
            return new DefaultClientValidationService(registry, validationOptions, sp);
        });

        return services;
    }

    /// <summary>
    /// Registers a client-side validation adapter factory for a specific
    /// <see cref="ValidationAttribute"/> type. The factory creates an
    /// <see cref="IClientValidationAdapter"/> that maps attribute properties
    /// to <c>data-val-*</c> HTML attributes.
    /// Later registrations for the same attribute type replace earlier ones,
    /// allowing built-in adapters to be overridden.
    /// </summary>
    /// <typeparam name="TAttribute">The validation attribute type to register an adapter for.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the adapter to.</param>
    /// <param name="factory">
    /// A factory delegate that creates an <see cref="IClientValidationAdapter"/>
    /// from the attribute instance.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddClientValidationAdapter&lt;CustomAttribute&gt;(
    ///     attribute =&gt; new CustomAttributeAdapter(attribute));
    /// </code>
    /// </example>
    public static IServiceCollection AddClientValidationAdapter<TAttribute>(
        this IServiceCollection services,
        Func<TAttribute, IClientValidationAdapter> factory)
        where TAttribute : ValidationAttribute
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

        services.Configure<ClientValidationAdapterRegistry>(registry =>
            registry.AddAdapter(factory));

        return services;
    }

    /// <summary>
    /// Registers built-in adapters for standard validation attributes into the
    /// <see cref="ClientValidationAdapterRegistry"/>.
    /// </summary>
    private sealed class BuiltInAdapterRegistration : IConfigureOptions<ClientValidationAdapterRegistry>
    {
        public void Configure(ClientValidationAdapterRegistry registry)
        {
            registry.AddAdapter<RequiredAttribute>(_ => new RequiredClientAdapter());
            registry.AddAdapter<StringLengthAttribute>(a => new StringLengthClientAdapter(a));
            registry.AddAdapter<RangeAttribute>(a => new RangeClientAdapter(a));
            registry.AddAdapter<MinLengthAttribute>(a => new MinLengthClientAdapter(a));
            registry.AddAdapter<MaxLengthAttribute>(a => new MaxLengthClientAdapter(a));
            registry.AddAdapter<RegularExpressionAttribute>(a => new RegexClientAdapter(a));
            registry.AddAdapter<EmailAddressAttribute>(_ => new DataTypeClientAdapter("data-val-email"));
            registry.AddAdapter<UrlAttribute>(_ => new DataTypeClientAdapter("data-val-url"));
            registry.AddAdapter<CreditCardAttribute>(_ => new DataTypeClientAdapter("data-val-creditcard"));
            registry.AddAdapter<PhoneAttribute>(_ => new DataTypeClientAdapter("data-val-phone"));
            registry.AddAdapter<CompareAttribute>(a => new CompareClientAdapter(a));
            registry.AddAdapter<FileExtensionsAttribute>(a => new FileExtensionsClientAdapter(a));
        }
    }
}
