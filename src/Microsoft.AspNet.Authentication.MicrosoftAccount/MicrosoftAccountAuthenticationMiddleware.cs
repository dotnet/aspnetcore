// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.DataProtection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Authentication.MicrosoftAccount
{
    /// <summary>
    /// An ASP.NET middleware for authenticating users using the Microsoft Account service.
    /// </summary>
    public class MicrosoftAccountAuthenticationMiddleware : OAuthAuthenticationMiddleware<MicrosoftAccountAuthenticationOptions, IMicrosoftAccountAuthenticationNotifications>
    {
        /// <summary>
        /// Initializes a new <see cref="MicrosoftAccountAuthenticationMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the HTTP pipeline to invoke.</param>
        /// <param name="dataProtectionProvider"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="options">Configuration options for the middleware.</param>
        public MicrosoftAccountAuthenticationMiddleware(
            [NotNull] RequestDelegate next,
            [NotNull] IDataProtectionProvider dataProtectionProvider,
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] IUrlEncoder encoder,
            [NotNull] IOptions<ExternalAuthenticationOptions> externalOptions,
            [NotNull] IOptions<MicrosoftAccountAuthenticationOptions> options,
            ConfigureOptions<MicrosoftAccountAuthenticationOptions> configureOptions = null)
            : base(next, dataProtectionProvider, loggerFactory, encoder, externalOptions, options, configureOptions)
        {
            if (Options.Notifications == null)
            {
                Options.Notifications = new MicrosoftAccountAuthenticationNotifications();
            }
            if (Options.Scope.Count == 0)
            {
                // LiveID requires a scope string, so if the user didn't set one we go for the least possible.
                // TODO: Should we just add these by default when we create the Options?
                Options.Scope.Add("wl.basic");
            }
        }

        /// <summary>
        /// Provides the <see cref="AuthenticationHandler"/> object for processing authentication-related requests.
        /// </summary>
        /// <returns>An <see cref="AuthenticationHandler"/> configured with the <see cref="MicrosoftAccountAuthenticationOptions"/> supplied to the constructor.</returns>
        protected override AuthenticationHandler<MicrosoftAccountAuthenticationOptions> CreateHandler()
        {
            return new MicrosoftAccountAuthenticationHandler(Backchannel);
        }
    }
}
