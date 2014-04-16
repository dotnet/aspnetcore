// -----------------------------------------------------------------------
// <copyright file="AuthenticationManager.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
        AuthenticationType _authTypes;

        internal AuthenticationManager(WebListener listener)
        {
            _server = listener;
            _authTypes = AuthenticationType.None;
        }

        #region Properties

        public AuthenticationType AuthenticationTypes
        {
            get
            {
                return _authTypes;
            }
            set
            {
                _authTypes = value;
                SetServerSecurity();
            }
        }

        #endregion Properties

        private unsafe void SetServerSecurity()
        {
            UnsafeNclNativeMethods.HttpApi.HTTP_SERVER_AUTHENTICATION_INFO authInfo =
                new UnsafeNclNativeMethods.HttpApi.HTTP_SERVER_AUTHENTICATION_INFO();

            authInfo.Flags = UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
            authInfo.AuthSchemes = (UnsafeNclNativeMethods.HttpApi.HTTP_AUTH_TYPES)_authTypes;

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

        internal void SetAuthenticationChallenge(Response response)
        {
            if (_authTypes == AuthenticationType.None)
            {
                return;
            }

            IList<string> challenges = new List<string>();

            // Order by strength.
            if ((_authTypes & AuthenticationType.Kerberos) == AuthenticationType.Kerberos)
            {
                challenges.Add("Kerberos");
            }
            if ((_authTypes & AuthenticationType.Negotiate) == AuthenticationType.Negotiate)
            {
                challenges.Add("Negotiate");
            }
            if ((_authTypes & AuthenticationType.Ntlm) == AuthenticationType.Ntlm)
            {
                challenges.Add("NTLM");
            }
            if ((_authTypes & AuthenticationType.Digest) == AuthenticationType.Digest)
            {
                // TODO:
                throw new NotImplementedException("Digest challenge generation has not been implemented.");
                // challenges.Add("Digest");
            }
            if ((_authTypes & AuthenticationType.Basic) == AuthenticationType.Basic)
            {
                // TODO: Realm
                challenges.Add("Basic");
            }

            // Append to the existing header, if any. Some clients (IE, Chrome) require each challenges to be sent on their own line/header.
            string[] oldValues;
            string[] newValues;
            if (response.Headers.TryGetValue(HttpKnownHeaderNames.WWWAuthenticate, out oldValues))
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
            response.Headers[HttpKnownHeaderNames.WWWAuthenticate] = newValues;
        }
    }
}
