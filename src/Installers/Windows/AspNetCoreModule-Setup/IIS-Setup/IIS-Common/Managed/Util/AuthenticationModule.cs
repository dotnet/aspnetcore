// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Net;
using System.Text;

namespace Microsoft.WebMatrix.Utility
{
    /// <summary>
    /// This AuthenticationModule implements basic authentication but uses UTF8 Encoding to support international characters.
    ///    Unfortunately the System.Net implementation uses the Default encoding which breaks with them.
    /// </summary>
    internal class AuthenticationModule : IAuthenticationModule
    {
        private const string AuthenticationTypeName = "Basic";
        private static AuthenticationModule _module = null;
        private static object _lock = new object();

        public static void InstantiateIfNeeded()
        {
            lock (_lock)
            {
                if (_module == null)
                {
                    _module = new AuthenticationModule();
                }
            }
        }

        private AuthenticationModule()
        {
            AuthenticationManager.Unregister(AuthenticationTypeName);
            AuthenticationManager.Register(this);
        }

        string IAuthenticationModule.AuthenticationType
        {
            get
            {
                return AuthenticationTypeName;
            }
        }

        bool IAuthenticationModule.CanPreAuthenticate
        {
            get
            {
                return true;
            }
        }

        Authorization IAuthenticationModule.Authenticate(string challenge, WebRequest request, ICredentials credentials)
        {
            HttpWebRequest httpWebRequest = request as HttpWebRequest;
            if (httpWebRequest == null)
            {
                return null;
            }

            // Verify that the challenge is a Basic Challenge
            if (challenge == null || !challenge.StartsWith(AuthenticationTypeName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return Authenticate(httpWebRequest, credentials);
        }

        Authorization IAuthenticationModule.PreAuthenticate(WebRequest request, ICredentials credentials)
        {
            HttpWebRequest httpWebRequest = request as HttpWebRequest;

            if (httpWebRequest == null)
            {
                return null;
            }

            return Authenticate(httpWebRequest, credentials);
        }

        private Authorization Authenticate(HttpWebRequest httpWebRequest, ICredentials credentials)
        {
            if (credentials == null)
            {
                return null;
            }

            // Get the username and password from the credentials
            NetworkCredential nc = credentials.GetCredential(httpWebRequest.RequestUri, AuthenticationTypeName);
            if (nc == null)
            {
                return null;
            }

            ICredentialPolicy policy = AuthenticationManager.CredentialPolicy;
            if (policy != null && !policy.ShouldSendCredential(httpWebRequest.RequestUri, httpWebRequest, nc, this))
            {
                return null;
            }

            string domain = nc.Domain;

            string basicTicket = (!String.IsNullOrEmpty(domain) ? (domain + "\\") : "") + nc.UserName + ":" + nc.Password;
            byte[] bytes = Encoding.UTF8.GetBytes(basicTicket);

            string header = AuthenticationTypeName + " " + Convert.ToBase64String(bytes);
            return new Authorization(header, true);
        }
    }
}