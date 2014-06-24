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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Principal;

namespace Microsoft.Net.Server
{
    // See the native HTTP_SERVER_AUTHENTICATION_INFO structure documentation for additional information.
    // http://msdn.microsoft.com/en-us/library/windows/desktop/aa364638(v=vs.85).aspx

    /// <summary>
    /// Exposes the Http.Sys authentication configurations.
    /// </summary>
    public sealed class AuthenticationManager
    {
#if NET45
        private static readonly int AuthInfoSize =
            Marshal.SizeOf(typeof(UnsafeNclNativeMethods.HttpApi.HTTP_SERVER_AUTHENTICATION_INFO));
#else
        private static readonly int AuthInfoSize =
            Marshal.SizeOf<UnsafeNclNativeMethods.HttpApi.HTTP_SERVER_AUTHENTICATION_INFO>();
#endif

        private WebListener _server;
        private AuthenticationTypes _authTypes;

        internal AuthenticationManager(WebListener listener)
        {
            _server = listener;
            _authTypes = AuthenticationTypes.AllowAnonymous;
        }

        #region Properties

        public AuthenticationTypes AuthenticationTypes
        {
            get
            {
                return _authTypes;
            }
            set
            {
                if (_authTypes == AuthenticationTypes.None)
                {
                    throw new ArgumentException("value", "'None' is not a valid authentication type. Use 'AllowAnonymous' instead.");
                }
                _authTypes = value;
                SetServerSecurity();
            }
        }

        internal bool AllowAnonymous
        {
            get
            {
                return ((_authTypes & AuthenticationTypes.AllowAnonymous) == AuthenticationTypes.AllowAnonymous);
            }
        }

        #endregion Properties

        private unsafe void SetServerSecurity()
        {
            UnsafeNclNativeMethods.HttpApi.HTTP_SERVER_AUTHENTICATION_INFO authInfo =
                new UnsafeNclNativeMethods.HttpApi.HTTP_SERVER_AUTHENTICATION_INFO();

            authInfo.Flags = UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
            var authTypes = (UnsafeNclNativeMethods.HttpApi.HTTP_AUTH_TYPES)(_authTypes & ~AuthenticationTypes.AllowAnonymous);
            if (authTypes != UnsafeNclNativeMethods.HttpApi.HTTP_AUTH_TYPES.NONE)
            {
                authInfo.AuthSchemes = authTypes;

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

        internal static IList<string> GenerateChallenges(AuthenticationTypes authTypes)
        {
            IList<string> challenges = new List<string>();

            if (authTypes == AuthenticationTypes.None)
            {
                return challenges;
            }

            // Order by strength.
            if ((authTypes & AuthenticationTypes.Kerberos) == AuthenticationTypes.Kerberos)
            {
                challenges.Add("Kerberos");
            }
            if ((authTypes & AuthenticationTypes.Negotiate) == AuthenticationTypes.Negotiate)
            {
                challenges.Add("Negotiate");
            }
            if ((authTypes & AuthenticationTypes.NTLM) == AuthenticationTypes.NTLM)
            {
                challenges.Add("NTLM");
            }
            /*if ((_authTypes & AuthenticationTypes.Digest) == AuthenticationTypes.Digest)
            {
                // TODO:
                throw new NotImplementedException("Digest challenge generation has not been implemented.");
                // challenges.Add("Digest");
            }*/
            if ((authTypes & AuthenticationTypes.Basic) == AuthenticationTypes.Basic)
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
                // TODO: We need a better header API that just lets us append values.
                // Append to the existing header, if any. Some clients (IE, Chrome) require each challenges to be sent on their own line/header.
                string[] oldValues;
                string[] newValues;
                if (context.Response.Headers.TryGetValue(HttpKnownHeaderNames.WWWAuthenticate, out oldValues))
                {
                    newValues = new string[oldValues.Length + challenges.Count];
                    Array.Copy(oldValues, newValues, oldValues.Length);
                    challenges.CopyTo(newValues, oldValues.Length);
                }
                else
                {
                    newValues = new string[challenges.Count];
                    challenges.CopyTo(newValues, 0);
                }
                context.Response.Headers[HttpKnownHeaderNames.WWWAuthenticate] = newValues;
            }
        }

        internal static unsafe bool CheckAuthenticated(UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_INFO* requestInfo)
        {
            if (requestInfo != null
                && requestInfo->InfoType == UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeAuth
                && requestInfo->pInfo->AuthStatus == UnsafeNclNativeMethods.HttpApi.HTTP_AUTH_STATUS.HttpAuthStatusSuccess)
            {
#if NET45
                return true;
#endif
            }
            return false;
        }

        internal static unsafe ClaimsPrincipal GetUser(UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_INFO* requestInfo)
        {
            if (requestInfo != null
                && requestInfo->InfoType == UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeAuth
                && requestInfo->pInfo->AuthStatus == UnsafeNclNativeMethods.HttpApi.HTTP_AUTH_STATUS.HttpAuthStatusSuccess)
            {
#if NET45
                return new WindowsPrincipal(new WindowsIdentity(requestInfo->pInfo->AccessToken,
                    GetAuthTypeFromRequest(requestInfo->pInfo->AuthType).ToString()));
#endif
            }
            return new ClaimsPrincipal(new ClaimsIdentity()); // Anonymous / !IsAuthenticated
        }

        private static AuthenticationTypes GetAuthTypeFromRequest(UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_AUTH_TYPE input)
        {
            switch (input)
            {
                case UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeBasic:
                    return AuthenticationTypes.Basic;
                // case UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeDigest:
                //  return AuthenticationTypes.Digest;
                case UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeNTLM:
                    return AuthenticationTypes.NTLM;
                case UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeNegotiate:
                    return AuthenticationTypes.Negotiate;
                case UnsafeNclNativeMethods.HttpApi.HTTP_REQUEST_AUTH_TYPE.HttpRequestAuthTypeKerberos:
                    return AuthenticationTypes.Kerberos;
                default:
                    throw new NotImplementedException(input.ToString());
            }
        }
    }
}
