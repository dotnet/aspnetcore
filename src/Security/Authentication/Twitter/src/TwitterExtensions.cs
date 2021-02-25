// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure Twitter OAuth authentication.
    /// </summary>
    public static class TwitterExtensions
    {
        /// <summary>
        /// Adds Twitter OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
        /// The default scheme is specified by <see cref="TwitterDefaults.AuthenticationScheme"/>.
        /// <para>
        /// Facebook authentication allows application users to sign in with their Facebook account.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder)
            => builder.AddTwitter(TwitterDefaults.AuthenticationScheme, _ => { });

        /// <summary>
        /// Adds Twitter OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
        /// The default scheme is specified by <see cref="TwitterDefaults.AuthenticationScheme"/>.
        /// <para>
        /// Facebook authentication allows application users to sign in with their Facebook account.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="configureOptions">A delegate to configure <see cref="TwitterOptions"/>.</param>
        /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder, Action<TwitterOptions> configureOptions)
            => builder.AddTwitter(TwitterDefaults.AuthenticationScheme, configureOptions);

        /// <summary>
        /// Adds Twitter OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
        /// The default scheme is specified by <see cref="TwitterDefaults.AuthenticationScheme"/>.
        /// <para>
        /// Facebook authentication allows application users to sign in with their Facebook account.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme">The authentication scheme.</param>
        /// <param name="configureOptions">A delegate to configure <see cref="TwitterOptions"/>.</param>
        /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder, string authenticationScheme, Action<TwitterOptions> configureOptions)
            => builder.AddTwitter(authenticationScheme, TwitterDefaults.DisplayName, configureOptions);

        /// <summary>
        /// Adds Twitter OAuth-based authentication to <see cref="AuthenticationBuilder"/> using the default scheme.
        /// The default scheme is specified by <see cref="TwitterDefaults.AuthenticationScheme"/>.
        /// <para>
        /// Facebook authentication allows application users to sign in with their Facebook account.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme">The authentication scheme.</param>
        /// <param name="displayName">A display name for the authentication handler.</param>
        /// <param name="configureOptions">A delegate to configure <see cref="TwitterOptions"/>.</param>
        /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
        public static AuthenticationBuilder AddTwitter(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<TwitterOptions> configureOptions)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TwitterOptions>, TwitterPostConfigureOptions>());
            return builder.AddRemoteScheme<TwitterOptions, TwitterHandler>(authenticationScheme, displayName, configureOptions);
        }
    }
}
