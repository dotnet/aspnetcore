// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(Microsoft.AspNetCore.AzureKeyVault.HostingStartup.AzureKeyVaultHostingStartup))]

namespace Microsoft.AspNetCore.AzureKeyVault.HostingStartup
{
    /// <summary>
    /// A dynamic KeyVault lightup experience
    /// </summary>
    public class AzureKeyVaultHostingStartup : IHostingStartup
    {
        private const string HostingStartupName = "KeyVault";
        private const string ConfigurationFeatureName = "ConfigurationEnabled";
        private const string ConfigurationVaultName = "ConfigurationVault";
        private const string DataProtectionFeatureName = "DataProtectionEnabled";
        private const string DataProtectionKeyName = "DataProtectionKey";

        /// <inheritdoc />
        public void Configure(IWebHostBuilder builder)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var authenticationCallback = new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback);
            var keyVaultClient = new KeyVaultClient(authenticationCallback);

            var baseConfiguration = HostingStartupConfigurationExtensions.GetBaseConfiguration();

            builder.ConfigureServices((context, collection) =>
            {
                var configuration = new ConfigurationBuilder()
                    .AddConfiguration(baseConfiguration)
                    .AddConfiguration(context.Configuration)
                    .Build();

                if (configuration.IsEnabled(HostingStartupName, DataProtectionFeatureName) &&
                    configuration.TryGetOption(HostingStartupName, DataProtectionKeyName, out var protectionKey))
                {
                    AddDataProtection(collection, keyVaultClient, protectionKey);
                }
            });

            if (baseConfiguration.IsEnabled(HostingStartupName, ConfigurationFeatureName) &&
                baseConfiguration.TryGetOption(HostingStartupName, ConfigurationVaultName, out var vault))
            {
                builder.ConfigureAppConfiguration((context, configurationBuilder) =>
                {
                    AddConfiguration(configurationBuilder, keyVaultClient, vault);
                });
            }
        }

        internal virtual void AddDataProtection(IServiceCollection serviceCollection, KeyVaultClient client, string protectionKey)
        {
            serviceCollection.AddDataProtection().ProtectKeysWithAzureKeyVault(client, protectionKey);
        }

        internal virtual void AddConfiguration(IConfigurationBuilder configurationBuilder, KeyVaultClient client, string keyVault)
        {
            configurationBuilder.AddAzureKeyVault(keyVault, client, new DefaultKeyVaultSecretManager());
        }
    }
}
