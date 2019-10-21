// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    // See the native HTTP_SERVER_AUTHENTICATION_INFO structure documentation for additional information.
    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa364638(v=vs.85).aspx

    /// <summary>
    /// Exposes the Http.Sys authentication configurations.
    /// </summary>
    public sealed class AuthenticationManager
    {
        private static readonly int AuthInfoSize =
            Marshal.SizeOf<HttpApiTypes.HTTP_SERVER_AUTHENTICATION_INFO>();

        private UrlGroup _urlGroup;
        private AuthenticationSchemes _authSchemes;
        private bool _allowAnonymous = true;

        internal AuthenticationManager()
        {
        }

        /// <summary>
        /// When attaching to an existing queue this setting must match the one used to create the queue.
        /// </summary>
        public AuthenticationSchemes Schemes
        {
            get { return _authSchemes; }
            set
            {
                _authSchemes = value;
                SetUrlGroupSecurity();
            }
        }

        /// <summary>
        /// Indicates if anonymous requests will be surfaced to the application or challenged by the server.
        /// The default value is true.
        /// </summary>
        public bool AllowAnonymous
        {
            get { return _allowAnonymous; }
            set { _allowAnonymous = value; }
        }

        /// <summary>
        /// If true the server should set HttpContext.User. If false the server will only provide an
        /// identity when explicitly requested by the AuthenticationScheme. The default is true.
        /// </summary>
        public bool AutomaticAuthentication { get; set; } = true;

        internal void SetUrlGroupSecurity(UrlGroup urlGroup)
        {
            Debug.Assert(_urlGroup == null, "SetUrlGroupSecurity called more than once.");
            _urlGroup = urlGroup;
            SetUrlGroupSecurity();
        }

        private unsafe void SetUrlGroupSecurity()
        {
            if (_urlGroup == null)
            {
                // Not started yet.
                return;
            }

            HttpApiTypes.HTTP_SERVER_AUTHENTICATION_INFO authInfo =
                new HttpApiTypes.HTTP_SERVER_AUTHENTICATION_INFO();

            authInfo.Flags = HttpApiTypes.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
            var authSchemes = (HttpApiTypes.HTTP_AUTH_TYPES)_authSchemes;
            if (authSchemes != HttpApiTypes.HTTP_AUTH_TYPES.NONE)
            {
                authInfo.AuthSchemes = authSchemes;

                // TODO:
                // NTLM auth sharing (on by default?) DisableNTLMCredentialCaching
                // Kerberos auth sharing (off by default?) HTTP_AUTH_EX_FLAG_ENABLE_KERBEROS_CREDENTIAL_CACHING
                // Mutual Auth - ReceiveMutualAuth
                // Digest domain and realm - HTTP_SERVER_AUTHENTICATION_DIGEST_PARAMS
                // Basic realm - HTTP_SERVER_AUTHENTICATION_BASIC_PARAMS

                IntPtr infoptr = new IntPtr(&authInfo);

                _urlGroup.SetProperty(
                    HttpApiTypes.HTTP_SERVER_PROPERTY.HttpServerAuthenticationProperty,
                    infoptr, (uint)AuthInfoSize);
            }
        }

        internal static IList<string> GenerateChallenges(AuthenticationSchemes authSchemes)
        {
            IList<string> challenges = new List<string>();

            if (authSchemes == AuthenticationSchemes.None)
            {
                return challenges;
            }

            // Order by strength.
            if ((authSchemes & AuthenticationSchemes.Kerberos) == AuthenticationSchemes.Kerberos)
            {
                challenges.Add("Kerberos");
            }
            if ((authSchemes & AuthenticationSchemes.Negotiate) == AuthenticationSchemes.Negotiate)
            {
                challenges.Add("Negotiate");
            }
            if ((authSchemes & AuthenticationSchemes.NTLM) == AuthenticationSchemes.NTLM)
            {
                challenges.Add("NTLM");
            }
            /*if ((_authSchemes & AuthenticationSchemes.Digest) == AuthenticationSchemes.Digest)
            {
                // TODO:
                throw new NotImplementedException("Digest challenge generation has not been implemented.");
                // challenges.Add("Digest");
            }*/
            if ((authSchemes & AuthenticationSchemes.Basic) == AuthenticationSchemes.Basic)
            {
                // TODO: Realm
                challenges.Add("Basic");
            }
            return challenges;
        }

        internal void SetAuthenticationChallenge(RequestContext context)
        {
            IList<string> challenges = GenerateChallenges(context.Response.AuthenticationChallenges);

            if (challenges.Count > 0)
            {
                context.Response.Headers[HttpKnownHeaderNames.WWWAuthenticate]
                    = StringValues.Concat(context.Response.Headers[HttpKnownHeaderNames.WWWAuthenticate], challenges.ToArray());
            }
        }
    }
}
