// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    internal class RelativeRedirectUriValidator : StrictRedirectUriValidator
    {
        public RelativeRedirectUriValidator(IAbsoluteUrlFactory absoluteUrlFactory)
        {
            if (absoluteUrlFactory == null)
            {
                throw new ArgumentNullException(nameof(absoluteUrlFactory));
            }

            AbsoluteUrlFactory = absoluteUrlFactory;
        }

        public IAbsoluteUrlFactory AbsoluteUrlFactory { get; }

        public override Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
        {
            if (IsLocalSPA(client))
            {
                return ValidateRelativeUris(requestedUri, client.RedirectUris);
            }
            else
            {
                return base.IsRedirectUriValidAsync(requestedUri, client);
            }
        }

        public override Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
        {
            if (IsLocalSPA(client))
            {
                return ValidateRelativeUris(requestedUri, client.PostLogoutRedirectUris);
            }
            else
            {
                return base.IsPostLogoutRedirectUriValidAsync(requestedUri, client);
            }
        }

        private static bool IsLocalSPA(Client client) =>
            client.Properties.TryGetValue(ApplicationProfilesPropertyNames.Profile, out var clientType) &&
            ApplicationProfiles.IdentityServerSPA == clientType;

        private Task<bool> ValidateRelativeUris(string requestedUri, IEnumerable<string> clientUris)
        {
            foreach (var url in clientUris)
            {
                if (Uri.IsWellFormedUriString(url, UriKind.Relative))
                {
                    var newUri = AbsoluteUrlFactory.GetAbsoluteUrl(url);
                    if (string.Equals(newUri, requestedUri, StringComparison.Ordinal))
                    {
                        return Task.FromResult(true);
                    }
                }
            }

            return Task.FromResult(false);
        }
    }
}
