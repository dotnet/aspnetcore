// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType is safe because user type is a reference type.")]
    public static IdentityBuilder AddDefaultTokenProviders(this IdentityBuilder builder)
    {
        var dataProtectionProviderType = typeof(DataProtectorTokenProvider<>).MakeGenericType(builder.UserType);
        var phoneNumberProviderType = typeof(PhoneNumberTokenProvider<>).MakeGenericType(builder.UserType);
        var emailTokenProviderType = typeof(EmailTokenProvider<>).MakeGenericType(builder.UserType);
        var authenticatorProviderType = typeof(AuthenticatorTokenProvider<>).MakeGenericType(builder.UserType);
        return builder.AddTokenProvider(TokenOptions.DefaultProvider, dataProtectionProviderType)
            .AddTokenProvider(TokenOptions.DefaultEmailProvider, emailTokenProviderType)
            .AddTokenProvider(TokenOptions.DefaultPhoneProvider, phoneNumberProviderType)
            .AddTokenProvider(TokenOptions.DefaultAuthenticatorProvider, authenticatorProviderType);
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType is safe because user type is a reference type.")]
    private static void AddSignInManagerDeps(this IdentityBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped(typeof(ISecurityStampValidator), typeof(SecurityStampValidator<>).MakeGenericType(builder.UserType));
        builder.Services.AddScoped(typeof(ITwoFactorSecurityStampValidator), typeof(TwoFactorSecurityStampValidator<>).MakeGenericType(builder.UserType));
    }

    /// <summary>
    /// Adds a <see cref="SignInManager{TUser}"/> for the <see cref="IdentityBuilder.UserType"/>.
    /// </summary>
    /// <param name="builder">The current <see cref="IdentityBuilder"/> instance.</param>
    /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType is safe because user type is a reference type.")]
    public static IdentityBuilder AddSignInManager(this IdentityBuilder builder)
    {
        builder.AddSignInManagerDeps();
        var managerType = typeof(SignInManager<>).MakeGenericType(builder.UserType);
        builder.Services.AddScoped(managerType);
        return builder;
    }

    /// <summary>
    /// Adds a <see cref="SignInManager{TUser}"/> for the <see cref="IdentityBuilder.UserType"/>.
    /// </summary>
    /// <typeparam name="TSignInManager">The type of the sign in manager to add.</typeparam>
    /// <param name="builder">The current <see cref="IdentityBuilder"/> instance.</param>
    /// <returns>The current <see cref="IdentityBuilder"/> instance.</returns>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "MakeGenericType is safe because user type is a reference type.")]
    public static IdentityBuilder AddSignInManager<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSignInManager>(this IdentityBuilder builder) where TSignInManager : class
    {
        builder.AddSignInManagerDeps();
        var managerType = typeof(SignInManager<>).MakeGenericType(builder.UserType);
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
