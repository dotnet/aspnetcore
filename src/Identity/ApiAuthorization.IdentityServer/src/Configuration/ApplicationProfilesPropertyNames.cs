// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Duende.IdentityServer.Models;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    /// <summary>
    /// Constants used for storing information about application profiles in the <see cref="Client.Properties"/> or <see cref="Resource.Properties"/>
    /// of a <see cref="Client"/> or <see cref="ApiResource"/> respectively.
    /// </summary>
    public static class ApplicationProfilesPropertyNames
    {
        /// <summary>
        /// Key to the Profile on <see cref="Client.Properties"/> or <see cref="Resource.Properties"/>. 
        /// The Profile value will be one of the constants in <see cref="ApplicationProfiles"/>.
        /// </summary>
        public const string Profile = nameof(Profile);

        /// <summary>
        /// Key to the Source on <see cref="Client.Properties"/> or <see cref="Resource.Properties"/>.
        /// The Source value will be Configuration if present.
        /// </summary>
        public const string Source = nameof(Source);

        /// <summary>
        /// Key to the Clients on <see cref="Resource.Properties"/>.
        /// The Clients value will be <c>*</c> to indicate that all clients are allowed to access this resource or a space separated list of
        /// the client ids that are allowed to access this resource.
        /// </summary>
        public const string Clients = nameof(Clients);
    }
}
