// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.DataProtection.Internal;

internal sealed class KeyManagementOptionsSetup : IConfigureOptions<KeyManagementOptions>
{
    private readonly IRegistryPolicyResolver? _registryPolicyResolver;
    private readonly ILoggerFactory _loggerFactory;

    public KeyManagementOptionsSetup()
        : this(NullLoggerFactory.Instance, registryPolicyResolver: null)
    {
    }

    public KeyManagementOptionsSetup(ILoggerFactory loggerFactory)
        : this(loggerFactory, registryPolicyResolver: null)
    {
    }

    public KeyManagementOptionsSetup(IRegistryPolicyResolver registryPolicyResolver)
        : this(NullLoggerFactory.Instance, registryPolicyResolver)
    {
    }

    public KeyManagementOptionsSetup(ILoggerFactory loggerFactory, IRegistryPolicyResolver? registryPolicyResolver)
    {
        _loggerFactory = loggerFactory;
        _registryPolicyResolver = registryPolicyResolver;
    }

    public void Configure(KeyManagementOptions options)
    {
        RegistryPolicy? context = null;
        if (_registryPolicyResolver != null)
        {
            context = _registryPolicyResolver.ResolvePolicy();
        }

        if (context != null)
        {
            if (context.DefaultKeyLifetime.HasValue)
            {
                options.NewKeyLifetime = TimeSpan.FromDays(context.DefaultKeyLifetime.Value);
            }

            options.AuthenticatedEncryptorConfiguration = context.EncryptorConfiguration;

            var escrowSinks = context.KeyEscrowSinks;
            if (escrowSinks != null)
            {
                foreach (var escrowSink in escrowSinks)
                {
                    options.KeyEscrowSinks.Add(escrowSink);
                }
            }
        }

        if (options.AuthenticatedEncryptorConfiguration == null)
        {
            options.AuthenticatedEncryptorConfiguration = new AuthenticatedEncryptorConfiguration();
        }

        options.AuthenticatedEncryptorFactories.Add(new CngGcmAuthenticatedEncryptorFactory(_loggerFactory));
        options.AuthenticatedEncryptorFactories.Add(new CngCbcAuthenticatedEncryptorFactory(_loggerFactory));
        options.AuthenticatedEncryptorFactories.Add(new ManagedAuthenticatedEncryptorFactory(_loggerFactory));
        options.AuthenticatedEncryptorFactories.Add(new AuthenticatedEncryptorFactory(_loggerFactory));
    }
}
