// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.DataProtection.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Data protection extensions for <see cref="IServiceProvider"/>.
/// </summary>
public static class DataProtectionUtilityExtensions
{
    /// <summary>
    /// Returns a unique identifier for this application.
    /// </summary>
    /// <param name="services">The application-level <see cref="IServiceProvider"/>.</param>
    /// <returns>A unique application identifier, or null if <paramref name="services"/> is null
    /// or cannot provide a unique application identifier.</returns>
    /// <remarks>
    /// <para>
    /// The returned identifier should be stable for repeated runs of this same application on
    /// this machine. Additionally, the identifier is only unique within the scope of a single
    /// machine, e.g., two different applications on two different machines may return the same
    /// value.
    /// </para>
    /// <para>
    /// This identifier may contain security-sensitive information such as physical file paths,
    /// configuration settings, or other machine-specific information. Callers should take
    /// special care not to disclose this information to untrusted entities.
    /// </para>
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static string? GetApplicationUniqueIdentifier(this IServiceProvider services)
    {
        string? discriminator = null;
        if (services != null)
        {
            discriminator = services.GetService<IApplicationDiscriminator>()?.Discriminator;
        }

        // Remove whitespace and homogenize empty -> null
        discriminator = discriminator?.Trim();
        return (string.IsNullOrEmpty(discriminator)) ? null : discriminator;
    }
}
