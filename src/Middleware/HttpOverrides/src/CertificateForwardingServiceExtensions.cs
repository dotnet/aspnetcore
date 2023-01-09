// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpOverrides;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for using certificate fowarding.
/// </summary>
public static class CertificateForwardingServiceExtensions
{
    /// <summary>
    /// Adds certificate forwarding to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">An action delegate to configure the provided <see cref="CertificateForwardingOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddCertificateForwarding(
        this IServiceCollection services,
        Action<CertificateForwardingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<CertificateForwardingOptions>().Validate(o => !string.IsNullOrEmpty(o.CertificateHeader), "CertificateForwarderOptions.CertificateHeader cannot be null or empty.");
        return services.Configure(configure);
    }
}
