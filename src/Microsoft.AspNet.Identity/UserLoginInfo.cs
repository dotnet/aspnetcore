// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Represents a linked login for a user (i.e. a local username/password or a facebook/google account
    /// </summary>
    public class UserLoginInfo
    {
        public UserLoginInfo(string loginProvider, string providerKey, string displayName)
        {
            LoginProvider = loginProvider;
            ProviderKey = providerKey;
            ProviderDisplayName = displayName;
        }

        /// <summary>
        ///     Provider for the linked login, i.e. Local, Facebook, Google, etc.
        /// </summary>
        public string LoginProvider { get; set; }

        /// <summary>
        ///     Key for the linked login at the provider
        /// </summary>
        public string ProviderKey { get; set; }

        /// <summary>
        ///     Display name for the provider
        /// </summary>
        public string ProviderDisplayName { get; set; }
    }
}