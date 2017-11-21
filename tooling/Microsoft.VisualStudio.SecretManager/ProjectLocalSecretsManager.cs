// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.SecretManager
{
    /// <summary>
    /// Provides an thread-safe access the secrets.json file based on the UserSecretsId property in a configured project.
    /// </summary>
    internal class ProjectLocalSecretsManager : Shell.IVsProjectSecrets, Shell.SVsProjectLocalSecrets
    {
        private const string UserSecretsPropertyName = "UserSecretsId";

        private readonly AsyncSemaphore _semaphore;
        private readonly IProjectPropertiesProvider _propertiesProvider;
        private readonly Lazy<IServiceProvider> _services;

        public ProjectLocalSecretsManager(IProjectPropertiesProvider propertiesProvider, Lazy<IServiceProvider> serviceProvider)
        {
            _propertiesProvider = propertiesProvider ?? throw new ArgumentNullException(nameof(propertiesProvider));
            _services = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _semaphore = new AsyncSemaphore(1);
        }

        public string SanitizeName(string name) => name;

        public IReadOnlyCollection<char> GetInvalidCharactersFrom(string name) => Array.Empty<char>();

        public async Task AddSecretAsync(string name, string value, CancellationToken cancellationToken = default)
        {
            EnsureKeyNameIsValid(name);
            await TaskScheduler.Default;

            using (await _semaphore.EnterAsync(cancellationToken))
            using (var store = await GetOrCreateStoreAsync(cancellationToken))
            {
                if (store.ContainsKey(name))
                {
                    throw new ArgumentException(Resources.Error_SecretAlreadyExists, nameof(name));
                }

                store.Set(name, value);
                await store.SaveAsync(cancellationToken);
            }
        }

        public async Task SetSecretAsync(string name, string value, CancellationToken cancellationToken = default)
        {
            EnsureKeyNameIsValid(name);
            await TaskScheduler.Default;

            using (await _semaphore.EnterAsync(cancellationToken))
            using (var store = await GetOrCreateStoreAsync(cancellationToken))
            {
                store.Set(name, value);
                await store.SaveAsync(cancellationToken);
            }
        }

        public async Task<string> GetSecretAsync(string name, CancellationToken cancellationToken = default)
        {
            EnsureKeyNameIsValid(name);
            await TaskScheduler.Default;

            using (await _semaphore.EnterAsync(cancellationToken))
            using (var store = await GetOrCreateStoreAsync(cancellationToken))
            {
                return store.Get(name);
            }
        }

        public async Task<IReadOnlyCollection<string>> GetSecretNamesAsync(CancellationToken cancellationToken = default)
        {
            await TaskScheduler.Default;

            using (await _semaphore.EnterAsync(cancellationToken))
            using (var store = await GetOrCreateStoreAsync(cancellationToken))
            {
                return store.ReadOnlyKeys;
            }
        }


        public async Task<IReadOnlyDictionary<string, string>> GetSecretsAsync(CancellationToken cancellationToken = default)
        {
            await TaskScheduler.Default;

            using (await _semaphore.EnterAsync(cancellationToken))
            using (var store = await GetOrCreateStoreAsync(cancellationToken))
            {
                return store.Values;
            }
        }

        public async Task<bool> RemoveSecretAsync(string name, CancellationToken cancellationToken = default)
        {
            EnsureKeyNameIsValid(name);
            await TaskScheduler.Default;

            using (await _semaphore.EnterAsync(cancellationToken))
            using (var store = await GetOrCreateStoreAsync(cancellationToken))
            {
                if (store.Remove(name))
                {
                    await store.SaveAsync(cancellationToken);
                    return true;
                }

                return false;
            }
        }

        private void EnsureKeyNameIsValid(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(nameof(name));
            }
        }

        private async Task<SecretStore> GetOrCreateStoreAsync(CancellationToken cancellationToken)
        {
            var userSecretsId = await _propertiesProvider.GetCommonProperties().GetEvaluatedPropertyValueAsync(UserSecretsPropertyName);

            if (string.IsNullOrEmpty(userSecretsId))
            {
                userSecretsId = Guid.NewGuid().ToString();
                await _propertiesProvider.GetCommonProperties().SetPropertyValueAsync(UserSecretsPropertyName, userSecretsId);
            }

            var store = new SecretStore(userSecretsId);
            await store.LoadAsync(cancellationToken);
            return store;
        }
    }
}
