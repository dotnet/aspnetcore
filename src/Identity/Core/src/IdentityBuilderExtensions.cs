// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Helper functions for configuring identity services.
/// </summary>
public static class IdentityBuilderExtensions
{
    /// <summary>
    /// Adds the default token providers used to generate tokens for reset passwords, change email
    /// and change telephone number operations, and for two factor authentication token generation.
    /// </summary>
    /// <param name="builder">The current <see cref="IdentityBuilder"/> instance.</param>
    /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType is safe because generic type and user type are reference types.")]
    public static IdentityBuilder AddDefaultTokenProviders(this IdentityBuilder builder)
    {
        var userType = builder.UserType;
        Debug.Assert(userType.IsClass);

        var dataProtectionProviderType = typeof(DataProtectorTokenProvider<>).MakeGenericType(userType);
        var phoneNumberProviderType = typeof(PhoneNumberTokenProvider<>).MakeGenericType(userType);
        var emailTokenProviderType = typeof(EmailTokenProvider<>).MakeGenericType(userType);
        var authenticatorProviderType = typeof(AuthenticatorTokenProvider<>).MakeGenericType(userType);
        return builder.AddTokenProvider(TokenOptions.DefaultProvider, dataProtectionProviderType)
            .AddTokenProvider(TokenOptions.DefaultEmailProvider, emailTokenProviderType)
            .AddTokenProvider(TokenOptions.DefaultPhoneProvider, phoneNumberProviderType)
            .AddTokenProvider(TokenOptions.DefaultAuthenticatorProvider, authenticatorProviderType);
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType is safe because generic type and user type are reference types.")]
    private static void AddSignInManagerDeps(this IdentityBuilder builder)
    {
        var userType = builder.UserType;
        Debug.Assert(userType.IsClass);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped(typeof(ISecurityStampValidator), typeof(SecurityStampValidator<>).MakeGenericType(userType));
        builder.Services.AddScoped(typeof(ITwoFactorSecurityStampValidator), typeof(TwoFactorSecurityStampValidator<>).MakeGenericType(userType));
    }

    /// <summary>
    /// Adds a <see cref="SignInManager{TUser}"/> for the <see cref="IdentityBuilder.UserType"/>.
    /// </summary>
    /// <param name="builder">The current <see cref="IdentityBuilder"/> instance.</param>
    /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType is safe because generic type and user type are reference types.")]
    public static IdentityBuilder AddSignInManager(this IdentityBuilder builder)
    {
        var userType = builder.UserType;
        Debug.Assert(userType.IsClass);

        builder.AddSignInManagerDeps();
        var managerType = typeof(SignInManager<>).MakeGenericType(userType);
        builder.Services.AddScoped(managerType);
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="SignInManager{TUser}"/> for the <see cref="IdentityBuilder.UserType"/>.
    /// </summary>
    /// <typeparam name="TSignInManager">The type of the sign in manager to add.</typeparam>
    /// <param name="builder">The current <see cref="IdentityBuilder"/> instance.</param>
    /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType is safe because generic type and user type are reference types.")]
    public static IdentityBuilder AddSignInManager<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSignInManager>(this IdentityBuilder builder) where TSignInManager : class
    {
        var userType = builder.UserType;
        Debug.Assert(userType.IsClass);

        builder.AddSignInManagerDeps();
        var managerType = typeof(SignInManager<>).MakeGenericType(userType);
        var customType = typeof(TSignInManager);
        if (!managerType.IsAssignableFrom(customType))
        {
            throw new InvalidOperationException(Resources.FormatInvalidManagerType(customType.Name, "SignInManager", builder.UserType.Name));
        }
        if (managerType != customType)
        {
            builder.Services.AddScoped(typeof(TSignInManager), services => services.GetRequiredService(managerType));
        }
        builder.Services.AddScoped(managerType, typeof(TSignInManager));
        return builder;
    }
}
