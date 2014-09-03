// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Security.OAuth
{
    /// <summary>
    /// Configuration options for <see cref="OAuthAuthenticationMiddleware"/>.
    /// </summary>
    public class OAuthAuthenticationOptions<TNotifications> : OAuthAuthenticationOptions where TNotifications : IOAuthAuthenticationNotifications
    {
        /// <summary>
        /// Initializes a new <see cref="OAuthAuthenticationOptions"/>.
        /// </summary>
        public OAuthAuthenticationOptions([NotNull] string authenticationType)
            : base(authenticationType)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="IOAuthAuthenticationNotifications"/> used to handle authentication events.
        /// </summary>
        public TNotifications Notifications { get; set; }
    }
}
