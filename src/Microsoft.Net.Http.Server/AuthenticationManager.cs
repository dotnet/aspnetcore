// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

// -----------------------------------------------------------------------
// <copyright file="AuthenticationManager.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Principal;

namespace Microsoft.Net.Http.Server
{
    // See the native HTTP_SERVER_AUTHENTICATION_INFO structure documentation for additional information.
    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa364638(v=vs.85).aspx

    /// <summary>
    /// Exposes the Http.Sys authentication configurations.
    /// </summary>
    public sealed class AuthenticationManager
    {
#if ASPNETCORE50
        private static readonly int AuthInfoSize =
            Marshal.SizeOf<UnsafeNclNativeMethods.HttpApi.HTTP_SERVER_AUTHENTICATION_INFO>();
#else
        private static readonly int AuthInfoSize =
            Marshal.SizeOf(typeof(UnsafeNclNativeMethods.HttpApi.HTTP_SERVER_AUTHENTICATION_INFO));
#endif

        private WebListener _server;
        private AuthenticationSchemes _authSchemes;

        internal AuthenticationManager(WebListener listener)
        {
            _server = listener;
            _authSchemes = AuthenticationSchemes.AllowAnonymous;
        }

        #region Properties

        public AuthenticationSchemes AuthenticationSchemes
        {
            get
            {
                return _authSchemes;
            }
            set
            {
                if (_authSchemes == AuthenticationSchemes.None)
                {
                    throw new ArgumentException("value", "'None' is not a valid authentication type. Use 'AllowAnonymous' instead.");
                }
                _authSchemes = value;
                SetServerSecurity();
            }
        }

        internal bool AllowAnonymous
        {
            get
            {
                return ((_authSchemes & AuthenticationSchemes.AllowAnonymous) == AuthenticationSchemes.AllowAnonymous);
            }
        }

        #endregion Properties

        private unsafe void SetServerSecurity()
        {
            UnsafeNclNativeMethods.HttpApi.HTTP_SERVER_AUTHENTICATION_INFO authInfo =
                new UnsafeNclNativeMethods.HttpApi.HTTP_SERVER_AUTHENTICATION_INFO();

            authInfo.Flags = UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
            var authSchemes = (UnsafeNclNativeMethods.HttpApi.HTTP_AUTH_TYPES)(_authSchemes & ~AuthenticationSchemes.AllowAnonymous);
            if (authSchemes != UnsafeNclNativeMethods.HttpApi.HTTP_AUTH_TYPES.NONE)
            {
                authInfo.AuthSchemes = authSchemes;

                // TODO:
                // NTLM auth sharing (on by default?) DisableNTLMCredentialCaching
                // Kerberos auth sharing (off by default?) HTTP_AUTH_EX_FLAG_ENABLE_KERBEROS_CREDENTIAL_CACHING
                // Mutual Auth - ReceiveMutualAuth
                // Digest domain and realm - HTTP_SERVER_AUTHENTICATION_DIGEST_PARAMS
                // Basic realm - HTTP_SERVER_AUTHENTICATION_BASIC_PARAMS

                IntPtr infoptr = new IntPtr(&authInfo);

                _server.SetUrlGroupProperty(
                    UnsafeNclNativeMethods.HttpApi.HTTP_SERVER_PROPERTY.HttpServerAuthenticationProperty,
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
            IList<string> challenges = GenerateChallenges(context.AuthenticationChallenges);

            if (challenges.Count > 0)
            {
                context.Response.Headers.AppendValues(HttpKnownHeaderNames.WWWAuthenticate, challenges.ToArray());
            }
        }

        internal static unsafe bool CheckAuthenticated(UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_INFO* requestInfo)
        {
            if (requestInfo != null
                && requestInfo->InfoType == UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeAuth
                && requestInfo->pInfo->AuthStatus == UnsafeNclNativeMethods.HttpApi.HTTP_AUTH_STATUS.HttpAuthStatusSuccess)
            {
                return true;
            }
            return false;
        }

        internal static unsafe ClaimsPrincipal GetUser(UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_INFO* requestInfo)
        {
            if (requestInfo != null
                && requestInfo->InfoType == UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeAuth
                && requestInfo->pInfo->AuthStatus == UnsafeNclNativeMethods.HttpApi.HTTP_AUTH_STATUS.HttpAuthStatusSuccess)
            {
                return new WindowsPrincipal(new WindowsIdentity(requestInfo->pInfo->AccessToken,
                    GetAuthTypeFromRequest(requestInfo->pInfo->AuthType).ToString()));
            }
            return new ClaimsPrincipal(new ClaimsIdentity()); // Anonymous / !IsAuthenticated
        }

        private static AuthenticationSchemes GetAuthTypeFromRequest(UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_AUTH_TYPE input)
        {
            switch (input)
            {
                case UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeBasic:
                    return AuthenticationSchemes.Basic;
                // case UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeDigest:
                //  return AuthenticationSchemes.Digest;
                case UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeNTLM:
                    return AuthenticationSchemes.NTLM;
                case UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeNegotiate:
                    return AuthenticationSchemes.Negotiate;
                case UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeKerberos:
                    return AuthenticationSchemes.Kerberos;
                default:
                    throw new NotImplementedException(input.ToString());
            }
        }
    }
}
