// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Authentication.OAuth
{
    /// <summary>
    /// Configuration options for <see cref="OAuthAuthenticationMiddleware"/>.
    /// </summary>
    public class OAuthAuthenticationOptions<TNotifications> : OAuthAuthenticationOptions where TNotifications : IOAuthAuthenticationNotifications
    {
        /// <summary>
        /// Gets or sets the <see cref="IOAuthAuthenticationNotifications"/> used to handle authentication events.
        /// </summary>
        public TNotifications Notifications { get; set; }
    }
}
