// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DefaultSigningCredentialsPolicyProvider : ISigningCredentialsPolicyProvider
    {
        private readonly IEnumerable<ISigningCredentialsSource> _sources;
        private readonly IHostingEnvironment _environment;
        private SigningCredentialsDescriptor[] _credentials;
        private readonly ITimeStampManager _timeStampManager;

        public DefaultSigningCredentialsPolicyProvider(
            IEnumerable<ISigningCredentialsSource> sources,
            ITimeStampManager timeStampManager,
            IHostingEnvironment environment)
        {
            _sources = sources;
            _timeStampManager = timeStampManager;
            _environment = environment;
        }

        public async Task<IEnumerable<SigningCredentialsDescriptor>> GetAllCredentialsAsync()
        {
            if (_credentials == null || CredentialsExpired())
            {
                // This has the potential to spin up multiple calls to RetrieveCredentials
                // we might consider an alternative pattern in which we hold a task in this
                // instance and upon expired credentials we lock, make the call to retrieve
                // credentials, swap the task on the instance, release the lock and then await.
                _credentials = await RetrieveCredentials();
            }

            return _credentials;

            async Task<SigningCredentialsDescriptor[]> RetrieveCredentials()
            {
                var credentialsFromSources = await Task.WhenAll(_sources.Select(s => s.GetCredentials()));

                var finalList = new List<SigningCredentialsDescriptor>();
                foreach (var credential in credentialsFromSources.SelectMany(c => c))
                {
                    if (!_environment.IsDevelopment() && credential.Id.StartsWith("Identity.Development"))
                    {
                        continue;
                    }

                    finalList.Add(credential);
                }

                return finalList.OrderBy(o => o.NotBefore).ThenBy(d => d.Expires).ToArray();
            }
        }

        private bool CredentialsExpired()
        {
            foreach (var credential in _credentials)
            {
                if (_timeStampManager.IsValidPeriod(credential.NotBefore, credential.Expires))
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<SigningCredentialsDescriptor> GetSigningCredentialsAsync()
        {
            var credentials = await GetAllCredentialsAsync();
            foreach (var credential in credentials)
            {
                if (_timeStampManager.IsValidPeriod(credential.NotBefore, credential.Expires))
                {
                    return credential;
                }
            }

            throw new InvalidOperationException("Could not find valid credentials to use for signing.");
        }
    }
}
