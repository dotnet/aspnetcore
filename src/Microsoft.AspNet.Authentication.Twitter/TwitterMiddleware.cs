// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.DataProtection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Authentication.Twitter
{
    /// <summary>
    /// ASP.NET middleware for authenticating users using Twitter
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Middleware are not disposable.")]
    public class TwitterMiddleware : AuthenticationMiddleware<TwitterOptions>
    {
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a <see cref="TwitterMiddleware"/>
        /// </summary>
        /// <param name="next">The next middleware in the HTTP pipeline to invoke</param>
        /// <param name="dataProtectionProvider"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="encoder"></param>
        /// <param name="sharedOptions"></param>
        /// <param name="options">Configuration options for the middleware</param>
        /// <param name="configureOptions"></param>
        public TwitterMiddleware(
            [NotNull] RequestDelegate next,
            [NotNull] IDataProtectionProvider dataProtectionProvider,
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] IUrlEncoder encoder,
            [NotNull] IOptions<SharedAuthenticationOptions> sharedOptions,
            [NotNull] TwitterOptions options)
            : base(next, options, loggerFactory, encoder)
        {
            if (string.IsNullOrEmpty(Options.ConsumerSecret))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(Options.ConsumerSecret)));
            }
            if (string.IsNullOrEmpty(Options.ConsumerKey))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(Options.ConsumerKey)));
            }

            if (Options.Events == null)
            {
                Options.Events = new TwitterEvents();
            }
            if (Options.StateDataFormat == null)
            {
                var dataProtector = dataProtectionProvider.CreateProtector(
                    typeof(TwitterMiddleware).FullName, Options.AuthenticationScheme, "v1");
                Options.StateDataFormat = new SecureDataFormat<RequestToken>(
                    new RequestTokenSerializer(),
                    dataProtector);
            }

            if (string.IsNullOrEmpty(Options.SignInScheme))
            {
                Options.SignInScheme = sharedOptions.Value.SignInScheme;
            }
            if (string.IsNullOrEmpty(Options.SignInScheme))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, "SignInScheme"));
            }

            _httpClient = new HttpClient(Options.BackchannelHttpHandler ?? new HttpClientHandler());
            _httpClient.Timeout = Options.BackchannelTimeout;
            _httpClient.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Microsoft ASP.NET Twitter middleware");
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;
        }

        /// <summary>
        /// Provides the <see cref="AuthenticationHandler"/> object for processing authentication-related requests.
        /// </summary>
        /// <returns>An <see cref="AuthenticationHandler"/> configured with the <see cref="TwitterOptions"/> supplied to the constructor.</returns>
        protected override AuthenticationHandler<TwitterOptions> CreateHandler()
        {
            return new TwitterHandler(_httpClient);
        }
    }
}
