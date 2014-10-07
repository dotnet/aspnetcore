// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Security.Infrastructure;

namespace Microsoft.AspNet.Security.OAuth
{
    /// <summary>
    /// Options class provides information needed to control Bearer Authentication middleware behavior
    /// </summary>
    public class OAuthBearerAuthenticationOptions : AuthenticationOptions
    {
        /// <summary>
        /// Creates an instance of bearer authentication options with default values.
        /// </summary>
        public OAuthBearerAuthenticationOptions() : base()
        {
            SystemClock = new SystemClock();
            AuthenticationType = OAuthBearerAuthenticationDefaults.AuthenticationType;
        }

        /// <summary>
        /// Determines what realm value is included when the bearer middleware adds a response header to an unauthorized request.
        /// If not assigned, the response header does not have a realm.
        /// </summary>
        public string Realm { get; set; }

        /// <summary>
        /// Specifies the full challenge to send to the client, and should start with "Bearer". If a challenge is provided then the
        /// Realm property is ignored. If no challenge is specified then one is created using "Bearer" and the value of the Realm
        /// property.
        /// </summary>
        public string Challenge { get; set; }

        /// <summary>
        /// The object provided by the application to process events raised by the bearer authentication middleware.
        /// The application may implement the interface fully, or it may create an instance of OAuthBearerAuthenticationProvider
        /// and assign delegates only to the events it wants to process.
        /// </summary>
        public IOAuthBearerAuthenticationNotifications Notifications { get; set; }

        /// <summary>
        /// The data format used to un-protect the information contained in the access token.
        /// If not provided by the application the default data protection provider depends on the host server. 
        /// The SystemWeb host on IIS will use ASP.NET machine key data protection, and HttpListener and other self-hosted
        /// servers will use DPAPI data protection. If a different access token
        /// provider or format is assigned, a compatible instance must be assigned to the OAuthAuthorizationServerOptions.AccessTokenProvider 
        /// and OAuthAuthorizationServerOptions.AccessTokenFormat of the authorization server.
        /// </summary>
        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; set; }

        /// <summary>
        /// Receives the bearer token the client application will be providing to web application. If not provided the token 
        /// produced on the server's default data protection by using the AccessTokenFormat. If a different access token
        /// provider or format is assigned, a compatible instance must be assigned to the OAuthAuthorizationServerOptions.AccessTokenProvider 
        /// and OAuthAuthorizationServerOptions.AccessTokenFormat of the authorization server.
        /// </summary>
        public IAuthenticationTokenProvider AccessTokenProvider { get; set; }

        /// <summary>
        /// Used to know what the current clock time is when calculating or validating token expiration. When not assigned default is based on
        /// DateTimeOffset.UtcNow. This is typically needed only for unit testing.
        /// </summary>
        public ISystemClock SystemClock { get; set; }
    }
}
