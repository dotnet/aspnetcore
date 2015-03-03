// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authentication.Twitter.Messages;

namespace Microsoft.AspNet.Authentication.Twitter
{
    /// <summary>
    /// Options for the Twitter authentication middleware.
    /// </summary>
    public class TwitterAuthenticationOptions : AuthenticationOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TwitterAuthenticationOptions"/> class.
        /// </summary>
        public TwitterAuthenticationOptions()
        {
            AuthenticationScheme = TwitterAuthenticationDefaults.AuthenticationScheme;
            Caption = AuthenticationScheme;
            CallbackPath = new PathString("/signin-twitter");
            BackchannelTimeout = TimeSpan.FromSeconds(60);
#if DNX451
            // Twitter lists its valid Subject Key Identifiers at https://dev.twitter.com/docs/security/using-ssl
            BackchannelCertificateValidator = new CertificateSubjectKeyIdentifierValidator(
                new[]
                {
                    "A5EF0B11CEC04103A34A659048B21CE0572D7D47", // VeriSign Class 3 Secure Server CA - G2
                    "0D445C165344C1827E1D20AB25F40163D8BE79A5", // VeriSign Class 3 Secure Server CA - G3
                    "5F60CF619055DF8443148A602AB2F57AF44318EF", // Symantec Class 3 Secure Server CA - G4
                });
#endif
        }

        /// <summary>
        /// Gets or sets the consumer key used to communicate with Twitter.
        /// </summary>
        /// <value>The consumer key used to communicate with Twitter.</value>
        public string ConsumerKey { get; set; }

        /// <summary>
        /// Gets or sets the consumer secret used to sign requests to Twitter.
        /// </summary>
        /// <value>The consumer secret used to sign requests to Twitter.</value>
        public string ConsumerSecret { get; set; }

        /// <summary>
        /// Gets or sets timeout value in milliseconds for back channel communications with Twitter.
        /// </summary>
        /// <value>
        /// The back channel timeout.
        /// </value>
        public TimeSpan BackchannelTimeout { get; set; }
#if DNX451
        /// <summary>
        /// Gets or sets the a pinned certificate validator to use to validate the endpoints used
        /// in back channel communications belong to Twitter.
        /// </summary>
        /// <value>
        /// The pinned certificate validator.
        /// </value>
        /// <remarks>If this property is null then the default certificate checks are performed,
        /// validating the subject name and if the signing chain is a trusted party.</remarks>
        public ICertificateValidator BackchannelCertificateValidator { get; set; }
#endif
        /// <summary>
        /// The HttpMessageHandler used to communicate with Twitter.
        /// This cannot be set at the same time as BackchannelCertificateValidator unless the value 
        /// can be downcast to a WebRequestHandler.
        /// </summary>
        public HttpMessageHandler BackchannelHttpHandler { get; set; }

        /// <summary>
        /// Get or sets the text that the user can display on a sign in user interface.
        /// </summary>
        public string Caption
        {
            get { return Description.Caption; }
            set { Description.Caption = value; }
        }

        /// <summary>
        /// The request path within the application's base path where the user-agent will be returned.
        /// The middleware will process this request when it arrives.
        /// Default value is "/signin-twitter".
        /// </summary>
        public PathString CallbackPath { get; set; }

        /// <summary>
        /// Gets or sets the authentication scheme corresponding to the middleware
        /// responsible of persisting user's identity after a successful authentication.
        /// This value typically corresponds to a cookie middleware registered in the Startup class.
        /// When omitted, <see cref="ExternalAuthenticationOptions.SignInScheme"/> is used as a fallback value.
        /// </summary>
        public string SignInScheme { get; set; }

        /// <summary>
        /// Gets or sets the type used to secure data handled by the middleware.
        /// </summary>
        public ISecureDataFormat<RequestToken> StateDataFormat { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ITwitterAuthenticationProvider"/> used to handle authentication events.
        /// </summary>
        public ITwitterAuthenticationNotifications Notifications { get; set; }
    }
}
