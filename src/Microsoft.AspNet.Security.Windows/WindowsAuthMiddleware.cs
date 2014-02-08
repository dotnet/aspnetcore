//------------------------------------------------------------------------------
// <copyright file="WindowsAuth.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Permissions;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Security.Windows
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// A middleware that performs Windows Authentication of the specified types.
    /// </summary>
    public sealed class WindowsAuthMiddleware
    {
        private Func<IDictionary<string, object>, AuthTypes> _authenticationDelegate;
        private AuthTypes _authenticationScheme = AuthTypes.Negotiate | AuthTypes.Ntlm | AuthTypes.Digest;
        private string _realm;
        private PrefixCollection _prefixes;
        private bool _unsafeConnectionNtlmAuthentication;
        private Func<IDictionary<string, object>, ExtendedProtectionPolicy> _extendedProtectionSelectorDelegate;
        private ExtendedProtectionPolicy _extendedProtectionPolicy;
        private ServiceNameStore _defaultServiceNames;

        private Hashtable _disconnectResults;         // ulong -> DisconnectAsyncResult
        private object _internalLock;

        internal Hashtable _uriPrefixes;
        private DigestCache _digestCache;

        private AppFunc _nextApp;

        // TODO: Support proxy auth
        // private bool _doProxyAuth;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nextApp"></param>
        public WindowsAuthMiddleware(AppFunc nextApp)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.HttpListener, this, "WindowsAuth", string.Empty);
            }

            _internalLock = new object();
            _defaultServiceNames = new ServiceNameStore();

            // default: no CBT checks on any platform (appcompat reasons); applies also to PolicyEnforcement 
            // config element
            _extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);
            _uriPrefixes = new Hashtable();
            _digestCache = new DigestCache();

            _nextApp = nextApp;

            if (Logging.On)
            {
                Logging.Exit(Logging.HttpListener, this, "WindowsAuth", string.Empty);
            }
        }

        /// <summary>
        /// Dynamically select the type of authentication to apply per request.
        /// </summary>
        public Func<IDictionary<string, object>, AuthTypes> AuthenticationSchemeSelectorDelegate
        {
            get
            {
                return _authenticationDelegate;
            }
            set
            {
                _authenticationDelegate = value;
            }
        }

        /// <summary>
        /// Dynamically select the type of extended protection to apply per request.
        /// </summary>
        public Func<IDictionary<string, object>, ExtendedProtectionPolicy> ExtendedProtectionSelectorDelegate
        {
            get
            {
                return _extendedProtectionSelectorDelegate;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                if (!ExtendedProtectionPolicy.OSSupportsExtendedProtection)
                {
                    throw new PlatformNotSupportedException(SR.GetString(SR.security_ExtendedProtection_NoOSSupport));
                }

                _extendedProtectionSelectorDelegate = value;
            }
        }

        /// <summary>
        /// Specifies which types of Windows authentication are enabled.
        /// </summary>
        public AuthTypes AuthenticationSchemes
        {
            get
            {
                return _authenticationScheme;
            }
            set
            {
                _authenticationScheme = value;
            }
        }

        /// <summary>
        /// Configures extended protection.
        /// </summary>
        public ExtendedProtectionPolicy ExtendedProtectionPolicy
        {
            get
            {
                return _extendedProtectionPolicy;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (!ExtendedProtectionPolicy.OSSupportsExtendedProtection && value.PolicyEnforcement == PolicyEnforcement.Always)
                {
                    throw new PlatformNotSupportedException(SR.GetString(SR.security_ExtendedProtection_NoOSSupport));
                }
                if (value.CustomChannelBinding != null)
                {
                    throw new ArgumentException(SR.GetString(SR.net_listener_cannot_set_custom_cbt), "CustomChannelBinding");
                }

                _extendedProtectionPolicy = value;
            }
        }

        /// <summary>
        /// Configures the service names for extended protection.
        /// </summary>
        public ServiceNameCollection DefaultServiceNames
        {
            get
            {
                return _defaultServiceNames.ServiceNames;
            }
        }

        /// <summary>
        /// The Realm for use in digest authentication.
        /// </summary>
        public string Realm
        {
            get
            {
                return _realm;
            }
            set
            {
                _realm = value;
            }
        }

        /// <summary>
        /// Enables authenticated connection sharing with NTLM.
        /// </summary>
        public bool UnsafeConnectionNtlmAuthentication
        {
            get
            {
                return _unsafeConnectionNtlmAuthentication;
            }

            set
            {
                if (_unsafeConnectionNtlmAuthentication == value)
                {
                    return;
                }
                lock (DisconnectResults.SyncRoot)
                {
                    if (_unsafeConnectionNtlmAuthentication == value)
                    {
                        return;
                    }
                    _unsafeConnectionNtlmAuthentication = value;
                    if (!value)
                    {
                        foreach (DisconnectAsyncResult result in DisconnectResults.Values)
                        {
                            result.AuthenticatedUser = null;
                        }
                    }
                }
            }
        }

        internal Hashtable DisconnectResults
        {
            get
            {
                if (_disconnectResults == null)
                {
                    Interlocked.CompareExchange(ref _disconnectResults, Hashtable.Synchronized(new Hashtable()), null);
                }
                return _disconnectResults;
            }
        }

        internal unsafe void AddPrefix(string uriPrefix)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.HttpListener, this, "AddPrefix", "uriPrefix:" + uriPrefix);
            }
            string registeredPrefix = null;
            try
            {
                if (uriPrefix == null)
                {
                    throw new ArgumentNullException("uriPrefix");
                }
                (new WebPermission(NetworkAccess.Accept, uriPrefix)).Demand();
                GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::AddPrefix() uriPrefix:" + uriPrefix);
                int i;
                if (string.Compare(uriPrefix, 0, "http://", 0, 7, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    i = 7;
                }
                else if (string.Compare(uriPrefix, 0, "https://", 0, 8, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    i = 8;
                }
                else
                {
                    throw new ArgumentException(SR.GetString(SR.net_listener_scheme), "uriPrefix");
                }
                bool inSquareBrakets = false;
                int j = i;
                while (j < uriPrefix.Length && uriPrefix[j] != '/' && (uriPrefix[j] != ':' || inSquareBrakets))
                {
                    if (uriPrefix[j] == '[')
                    {
                        if (inSquareBrakets)
                        {
                            j = i;
                            break;
                        }
                        inSquareBrakets = true;
                    }
                    if (inSquareBrakets && uriPrefix[j] == ']')
                    {
                        inSquareBrakets = false;
                    }
                    j++;
                }
                if (i == j)
                {
                    throw new ArgumentException(SR.GetString(SR.net_listener_host), "uriPrefix");
                }
                if (uriPrefix[uriPrefix.Length - 1] != '/')
                {
                    throw new ArgumentException(SR.GetString(SR.net_listener_slash), "uriPrefix");
                }
                registeredPrefix = uriPrefix[j] == ':' ? String.Copy(uriPrefix) : uriPrefix.Substring(0, j) + (i == 7 ? ":80" : ":443") + uriPrefix.Substring(j);
                fixed (char* pChar = registeredPrefix)
                {
                    i = 0;
                    while (pChar[i] != ':')
                    {
                        pChar[i] = (char)CaseInsensitiveAscii.AsciiToLower[(byte)pChar[i]];
                        i++;
                    }
                }
                GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::AddPrefix() mapped uriPrefix:" + uriPrefix + " to registeredPrefix:" + registeredPrefix);

                _uriPrefixes[uriPrefix] = registeredPrefix;
                _defaultServiceNames.Add(uriPrefix);
            }
            catch (Exception exception)
            {
                if (Logging.On)
                {
                    Logging.Exception(Logging.HttpListener, this, "AddPrefix", exception);
                }
                throw;
            }
            finally
            {
                if (Logging.On)
                {
                    Logging.Exit(Logging.HttpListener, this, "AddPrefix", "prefix:" + registeredPrefix);
                }
            }
        }

        internal PrefixCollection Prefixes
        {
            get
            {
                if (Logging.On)
                {
                    Logging.Enter(Logging.HttpListener, this, "Prefixes_get", string.Empty);
                }
                if (_prefixes == null)
                {
                    _prefixes = new PrefixCollection(this);
                }
                return _prefixes;
            }
        }

        internal bool RemovePrefix(string uriPrefix)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.HttpListener, this, "RemovePrefix", "uriPrefix:" + uriPrefix);
            }
            try
            {
                GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::RemovePrefix() uriPrefix:" + uriPrefix);
                if (uriPrefix == null)
                {
                    throw new ArgumentNullException("uriPrefix");
                }

                if (!_uriPrefixes.Contains(uriPrefix))
                {
                    return false;
                }

                _uriPrefixes.Remove(uriPrefix);
                _defaultServiceNames.Remove(uriPrefix);
            }
            catch (Exception exception)
            {
                if (Logging.On)
                {
                    Logging.Exception(Logging.HttpListener, this, "RemovePrefix", exception);
                }
                throw;
            }
            finally
            {
                if (Logging.On)
                {
                    Logging.Exit(Logging.HttpListener, this, "RemovePrefix", "uriPrefix:" + uriPrefix);
                }
            }
            return true;
        }

        internal void RemoveAll(bool clear)
        {
            if (Logging.On)
            {
                Logging.Enter(Logging.HttpListener, this, "RemoveAll", string.Empty);
            }
            try
            {
                // go through the uri list and unregister for each one of them
                if (_uriPrefixes.Count > 0)
                {
                    if (clear)
                    {
                        _uriPrefixes.Clear();
                        _defaultServiceNames.Clear();
                    }
                }
            }
            finally
            {
                if (Logging.On)
                {
                    Logging.Exit(Logging.HttpListener, this, "RemoveAll", string.Empty);
                }
            }
        }

        // old API, now private, and helper methods
        private void Dispose(bool disposing)
        {
            GlobalLog.Assert(disposing, "Dispose(bool) does nothing if called from the finalizer.");

            if (!disposing)
            {
                return;
            }

            try
            {
                _digestCache.Dispose();
            }
            finally
            {
                if (Logging.On)
                {
                    Logging.Exit(Logging.HttpListener, this, "Dispose", string.Empty);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public Task Invoke(IDictionary<string, object> env)
        {
            // Process the auth header, if any
            if (!TryHandleAuthentication(env))
            {
                // If failed and a 400/401/500 was sent.
                return Task.FromResult<object>(null);
            }

            // If passing through, register for OnSendingHeaders.  Add an auth header challenge on 401.
            var registerOnSendingHeaders = env.Get<Action<Action<object>, object>>(Constants.ServerOnSendingHeadersKey);
            if (registerOnSendingHeaders == null)
            {
                // This module requires OnSendingHeaders support.
                throw new PlatformNotSupportedException();
            }
            registerOnSendingHeaders(Set401Challenges, env);

            // Invoke the next item in the app chain
            return _nextApp(env);
        }

        // Returns true if auth completed successfully (or anonymous), false if there was an auth header
        // but processing it failed.
        private bool TryHandleAuthentication(IDictionary<string, object> env)
        {
            DisconnectAsyncResult disconnectResult;
            object connectionId = env.Get<object>(Constants.ServerConnectionIdKey, -1);
            string authorizationHeader = null;
            if (!TryGetIncomingAuthHeader(env, out authorizationHeader))
            {
                if (UnsafeConnectionNtlmAuthentication)
                {
                    disconnectResult = (DisconnectAsyncResult)DisconnectResults[connectionId];
                    if (disconnectResult != null)
                    {
                        WindowsPrincipal principal = disconnectResult.AuthenticatedUser;
                        if (principal != null)
                        {
                            // This connection has already been authenticated;
                            SetIdentity(env, principal, null);
                        }
                    }
                }

                return true; // Anonymous or UnsafeConnectionNtlmAuthentication
            }

            GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::HandleAuthentication() authorizationHeader:" + ValidationHelper.ToString(authorizationHeader));

            if (UnsafeConnectionNtlmAuthentication)
            {
                disconnectResult = (DisconnectAsyncResult)DisconnectResults[connectionId];
                // They sent an authorization header - destroy their previous credentials.
                GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::HandleAuthentication() clearing principal cache");
                if (disconnectResult != null)
                {
                    disconnectResult.AuthenticatedUser = null;
                }
            }

            try
            {
                AuthTypes headerScheme;
                string inBlob;
                if (!TryGetRecognizedAuthScheme(authorizationHeader, out headerScheme, out inBlob))
                {
                    return true; // Anonymous / pass through
                }
                Contract.Assert(headerScheme != AuthTypes.None);
                Contract.Assert(inBlob != null);

                GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::HandleAuthentication() Performing Authentication headerScheme:" + ValidationHelper.ToString(headerScheme));
                switch (headerScheme)
                {
                    case AuthTypes.Digest:
                        return TryAuthenticateWithDigest(env, inBlob);

                    case AuthTypes.Negotiate:
                    case AuthTypes.Ntlm:
                        string package = headerScheme == AuthTypes.Ntlm ? NegotiationInfoClass.NTLM : NegotiationInfoClass.Negotiate;
                        return TryAuthenticateWithNegotiate(env, package, inBlob);

                    default:
                        throw new NotImplementedException(headerScheme.ToString());
                }
            }
            catch (Exception)
            {
                SendError(env, HttpStatusCode.InternalServerError, null);
                return false;
            }
        }

        // TODO: Support proxy auth
        private bool TryGetIncomingAuthHeader(IDictionary<string, object> env, out string authorizationHeader)
        {
            IDictionary<string, string[]> headers = env.Get<IDictionary<string, string[]>>(Constants.RequestHeadersKey);
            authorizationHeader = headers.Get("Authorization");
            return !string.IsNullOrWhiteSpace(authorizationHeader);
        }

        private bool TryGetRecognizedAuthScheme(string authorizationHeader, out AuthTypes headerScheme, out string inBlob)
        {
            headerScheme = AuthTypes.None;

            int index;
            // Find the end of the scheme name.  Trust that HTTP.SYS parsed out just our header ok.
            for (index = 0; index < authorizationHeader.Length; index++)
            {
                if (authorizationHeader[index] == ' ' || authorizationHeader[index] == '\t' ||
                    authorizationHeader[index] == '\r' || authorizationHeader[index] == '\n')
                {
                    break;
                }
            }

            // Currently only allow one Authorization scheme/header per request.
            if (index < authorizationHeader.Length)
            {
                if ((AuthenticationSchemes & AuthTypes.Negotiate) != AuthTypes.None &&
                    string.Compare(authorizationHeader, 0, NegotiationInfoClass.Negotiate, 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    headerScheme = AuthTypes.Negotiate;
                }
                else if ((AuthenticationSchemes & AuthTypes.Ntlm) != AuthTypes.None &&
                    string.Compare(authorizationHeader, 0, NegotiationInfoClass.NTLM, 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    headerScheme = AuthTypes.Ntlm;
                }
                else if ((AuthenticationSchemes & AuthTypes.Digest) != AuthTypes.None &&
                    string.Compare(authorizationHeader, 0, NegotiationInfoClass.Digest, 0, index, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    headerScheme = AuthTypes.Digest;
                }
            }

            // Find the beginning of the blob.  Trust that HTTP.SYS parsed out just our header ok.
            for (index++; index < authorizationHeader.Length; index++)
            {
                if (authorizationHeader[index] != ' ' && authorizationHeader[index] != '\t' &&
                    authorizationHeader[index] != '\r' && authorizationHeader[index] != '\n')
                {
                    break;
                }
            }
            inBlob = index < authorizationHeader.Length ? authorizationHeader.Substring(index) : string.Empty;

            return headerScheme != AuthTypes.None;
        }

        // Returns true if successfully authenticated via Digest. Returns false if a 401 was sent.
        private bool TryAuthenticateWithDigest(IDictionary<string, object> env, string inBlob)
        {
            NTAuthentication context = null;
            IPrincipal principal = null;
            SecurityStatus statusCodeNew;
            ChannelBinding binding;
            string outBlob;
            HttpStatusCode httpError = HttpStatusCode.OK;
            string verb = env.Get<string>(Constants.RequestMethodKey);
            bool isSecureConnection = IsSecureConnection(env);
            // GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::HandleAuthentication() package:WDigest headerScheme:" + headerScheme);

            // WDigest had some weird behavior.  This is what I have discovered:
            // Local accounts don't work, only domain accounts.  The domain (i.e. REDMOND) is implied.  Not sure how it is chosen.
            // If the domain is specified and the credentials are correct, it works.  If they're not (domain, username or password):
            //      AcceptSecurityContext (GetOutgoingDigestBlob) returns success but with a bogus 4k challenge, and
            //      QuerySecurityContextToken (GetContextToken) fails with NoImpersonation.
            // If the domain isn't specified, AcceptSecurityContext returns NoAuthenticatingAuthority for a bad username,
            // and LogonDenied for a bad password.

            // Also interesting is that WDigest requires us to keep a reference to the previous context, but fails if we
            // actually pass it in!  (It't ok to pass it in for the first request, but not if nc > 1.)  For Whidbey,
            // we create a new context and associate it with the connection, just like NTLM, but instead of using it for
            // the next request on the connection, we always create a new context and swap the old one out.  As long
            // as we keep the old one around until after we authenticate with the new one, it works.  For this reason,
            // we also keep these contexts around past the lifetime of the connection, so that KeepAlive=false works.
            binding = GetChannelBinding(env, isSecureConnection, ExtendedProtectionPolicy);

            context = new NTAuthentication(true, NegotiationInfoClass.WDigest, null,
                GetContextFlags(ExtendedProtectionPolicy, isSecureConnection), binding);

            GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::HandleAuthentication() verb:" + verb + " context.IsValidContext:" + context.IsValidContext.ToString());

            outBlob = context.GetOutgoingDigestBlob(inBlob, verb, null, Realm, false, false, out statusCodeNew);
            GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::HandleAuthentication() GetOutgoingDigestBlob() returned IsCompleted:" + context.IsCompleted + " statusCodeNew:" + statusCodeNew + " outBlob:[" + outBlob + "]");

            // WDigest bug: sometimes when AcceptSecurityContext returns success, it provides a bogus, empty 4k buffer.
            // Ignore it.  (Should find out what's going on here from WDigest people.)
            if (statusCodeNew == SecurityStatus.OK)
            {
                outBlob = null;
            }

            IList<string> challenges = null;
            if (outBlob != null)
            {
                string challenge = NegotiationInfoClass.Digest + " " + outBlob;
                AddChallenge(ref challenges, challenge);
            }

            if (context.IsValidContext)
            {
                SafeCloseHandle userContext = null;
                try
                {
                    if (!CheckSpn(context, isSecureConnection, ExtendedProtectionPolicy))
                    {
                        httpError = HttpStatusCode.Unauthorized;
                    }
                    else
                    {
                        SetServiceName(env, context.ClientSpecifiedSpn);

                        userContext = context.GetContextToken(out statusCodeNew);
                        GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::HandleAuthentication() GetContextToken() returned:" + statusCodeNew.ToString());
                        if (statusCodeNew != SecurityStatus.OK)
                        {
                            httpError = HttpStatusFromSecurityStatus(statusCodeNew);
                        }
                        else if (userContext == null)
                        {
                            GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::HandleAuthentication() error: GetContextToken() returned:null statusCodeNew:" + statusCodeNew.ToString());
                            httpError = HttpStatusCode.Unauthorized;
                        }
                        else
                        {
                            GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::HandleAuthentication() creating new WindowsIdentity() from userContext:" + userContext.DangerousGetHandle().ToString("x8"));
                            principal = new WindowsPrincipal(CreateWindowsIdentity(userContext.DangerousGetHandle(), "Digest"/*DigestClient.AuthType*/, WindowsAccountType.Normal, true));
                            SetIdentity(env, principal, null);
                            _digestCache.SaveDigestContext(context);
                        }
                    }
                }
                finally
                {
                    if (userContext != null)
                    {
                        userContext.Dispose();
                    }
                }
            }
            else
            {
                httpError = HttpStatusFromSecurityStatus(statusCodeNew);
            }

            if (httpError != HttpStatusCode.OK)
            {
                SendError(env, httpError, challenges);
                return false;
            }
            return true;
        }

        // Negotiate or NTLM
        private bool TryAuthenticateWithNegotiate(IDictionary<string, object> env, string package, string inBlob)
        {
            object connectionId = env.Get<object>(Constants.ServerConnectionIdKey, null);
            if (connectionId == null)
            {
                // We need a connection ID from the server to correctly track in-progress auth.
                throw new PlatformNotSupportedException();
            }

            NTAuthentication oldContext = null, context;
            DisconnectAsyncResult disconnectResult = (DisconnectAsyncResult)DisconnectResults[connectionId];
            if (disconnectResult != null)
            {
                oldContext = disconnectResult.Session;
            }
            ChannelBinding binding;
            bool isSecureConnection = IsSecureConnection(env);
            byte[] bytes = null;
            HttpStatusCode httpError = HttpStatusCode.OK;
            bool error = false;
            string outBlob = null;

            if (oldContext != null && oldContext.Package == package)
            {
                context = oldContext;
            }
            else
            {
                binding = GetChannelBinding(env, isSecureConnection, ExtendedProtectionPolicy);

                context = new NTAuthentication(true, package, null,
                    GetContextFlags(ExtendedProtectionPolicy, isSecureConnection), binding);

                // Clean up old context
                if (oldContext != null)
                {
                    oldContext.CloseContext();
                }
            }

            try
            {
                bytes = Convert.FromBase64String(inBlob);
            }
            catch (FormatException)
            {
                GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::HandleAuthentication() FromBase64String threw a FormatException.");
                httpError = HttpStatusCode.BadRequest;
                error = true;
            }

            byte[] decodedOutgoingBlob = null;
            SecurityStatus statusCodeNew;
            if (!error)
            {
                decodedOutgoingBlob = context.GetOutgoingBlob(bytes, false, out statusCodeNew);
                GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::HandleAuthentication() GetOutgoingBlob() returned IsCompleted:" + context.IsCompleted + " statusCodeNew:" + statusCodeNew);
                error = !context.IsValidContext;
                if (error)
                {
                    // Bug #474228: SSPI Workaround
                    // If a client sends up a blob on the initial request, Negotiate returns SEC_E_INVALID_HANDLE
                    // when it should return SEC_E_INVALID_TOKEN.
                    if (statusCodeNew == SecurityStatus.InvalidHandle && oldContext == null && bytes != null && bytes.Length > 0)
                    {
                        statusCodeNew = SecurityStatus.InvalidToken;
                    }

                    httpError = HttpStatusFromSecurityStatus(statusCodeNew);
                }
            }

            if (decodedOutgoingBlob != null)
            {
                outBlob = Convert.ToBase64String(decodedOutgoingBlob);
            }

            if (!error)
            {
                if (context.IsCompleted)
                {
                    SafeCloseHandle userContext = null;
                    try
                    {
                        if (!CheckSpn(context, isSecureConnection, ExtendedProtectionPolicy))
                        {
                            httpError = HttpStatusCode.Unauthorized;
                        }
                        else
                        {
                            SetServiceName(env, context.ClientSpecifiedSpn);

                            userContext = context.GetContextToken(out statusCodeNew);
                            if (statusCodeNew != SecurityStatus.OK)
                            {
                                GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::HandleAuthentication() GetContextToken() failed with statusCodeNew:" + statusCodeNew.ToString());
                                httpError = HttpStatusFromSecurityStatus(statusCodeNew);
                            }
                            else
                            {
                                GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::HandleAuthentication() creating new WindowsIdentity() from userContext:" + userContext.DangerousGetHandle().ToString("x8"));
                                WindowsPrincipal windowsPrincipal = new WindowsPrincipal(CreateWindowsIdentity(userContext.DangerousGetHandle(), context.ProtocolName, WindowsAccountType.Normal, true));
                                SetIdentity(env, windowsPrincipal, outBlob);

                                // if appropriate, cache this credential on this connection
                                if (UnsafeConnectionNtlmAuthentication
                                    && context.ProtocolName.Equals(NegotiationInfoClass.NTLM, StringComparison.OrdinalIgnoreCase))
                                {
                                    // We may need to call WaitForDisconnect.
                                    if (disconnectResult == null)
                                    {
                                        RegisterForDisconnectNotification(env, out disconnectResult);
                                    }

                                    if (disconnectResult != null)
                                    {
                                        lock (DisconnectResults.SyncRoot)
                                        {
                                            if (UnsafeConnectionNtlmAuthentication)
                                            {
                                                disconnectResult.AuthenticatedUser = windowsPrincipal;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (userContext != null)
                        {
                            userContext.Dispose();
                        }
                    }
                    return true;
                }
                else
                {
                    // auth incomplete
                    if (disconnectResult == null)
                    {
                        RegisterForDisconnectNotification(env, out disconnectResult);

                        // Failed - send 500.
                        if (disconnectResult == null)
                        {
                            context.CloseContext();
                            SendError(env, HttpStatusCode.InternalServerError, null);
                            return false;
                        }
                    }

                    disconnectResult.Session = context;

                    string challenge = package;
                    if (!String.IsNullOrEmpty(outBlob))
                    {
                        challenge += " " + outBlob;
                    }
                    IList<string> challenges = null;
                    AddChallenge(ref challenges, challenge);
                    SendError(env, HttpStatusCode.Unauthorized, challenges);
                    return false;
                }
            }

            SendError(env, httpError, null);
            return false;
        }

        private void SetIdentity(IDictionary<string, object> env, IPrincipal principal, string mutualAuth)
        {
            env[Constants.ServerUserKey] = principal;
            if (!string.IsNullOrWhiteSpace(mutualAuth))
            {
                var responseHeaders = env.Get<IDictionary<string, string[]>>(Constants.ResponseHeadersKey);
                responseHeaders.Append(HttpKnownHeaderNames.WWWAuthenticate, mutualAuth);
            }
        }

        // For user info only
        private void SetServiceName(IDictionary<string, object> env, string serviceName)
        {
            if (!string.IsNullOrWhiteSpace(serviceName))
            {
                env[Constants.SslSpnKey] = serviceName;
            }
        }

        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.ControlPrincipal)]
        internal static WindowsIdentity CreateWindowsIdentity(IntPtr userToken, string type, WindowsAccountType acctType, bool isAuthenticated)
        {
            return new WindowsIdentity(userToken, type, acctType, isAuthenticated);
        }

        // On a 401 response, set any appropriate challenges
        private void Set401Challenges(object state)
        {
            var env = (IDictionary<string, object>)state;
            var responseHeaders = env.Get<IDictionary<string, string[]>>(Constants.ResponseHeadersKey);

            // We use the cached results from the delegates so that we don't have to call them again here.
            NTAuthentication newContext;
            IList<string> challenges = BuildChallenge(env, AuthenticationSchemes, out newContext, ExtendedProtectionPolicy);

            // null == Anonymous
            if (challenges != null)
            {
                // Digest challenge, keep it alive for 10s - 5min.
                if (newContext != null) 
                {
                    _digestCache.SaveDigestContext(newContext);
                }

                responseHeaders.Append(HttpKnownHeaderNames.WWWAuthenticate, challenges);
            }
        }

        private static bool IsSecureConnection(IDictionary<string, object> env)
        {
            return "https".Equals(env.Get<string>(Constants.RequestSchemeKey, "http"), StringComparison.OrdinalIgnoreCase);
        }

        private static bool ScenarioChecksChannelBinding(bool isSecureConnection, ProtectionScenario scenario)
        {
            return (isSecureConnection && scenario == ProtectionScenario.TransportSelected);
        }

        private ChannelBinding GetChannelBinding(IDictionary<string, object> env, bool isSecureConnection, ExtendedProtectionPolicy policy)
        {
            if (policy.PolicyEnforcement == PolicyEnforcement.Never)
            {
                if (Logging.On)
                {
                    Logging.PrintInfo(Logging.HttpListener, this, SR.GetString(SR.net_log_listener_no_cbt_disabled));
                }
                return null;
            }

            if (!isSecureConnection)
            {
                if (Logging.On)
                {
                    Logging.PrintInfo(Logging.HttpListener, this, SR.GetString(SR.net_log_listener_no_cbt_http));
                }
                return null;
            }

            if (!ExtendedProtectionPolicy.OSSupportsExtendedProtection)
            {
                GlobalLog.Assert(policy.PolicyEnforcement != PolicyEnforcement.Always, "User managed to set PolicyEnforcement.Always when the OS does not support extended protection!");
                if (Logging.On)
                {
                    Logging.PrintInfo(Logging.HttpListener, this, SR.GetString(SR.net_log_listener_no_cbt_platform));
                }
                return null;
            }

            if (policy.ProtectionScenario == ProtectionScenario.TrustedProxy)
            {
                if (Logging.On)
                {
                    Logging.PrintInfo(Logging.HttpListener, this, SR.GetString(SR.net_log_listener_no_cbt_trustedproxy));
                }
                return null;
            }

            ChannelBinding result = env.Get<ChannelBinding>(Constants.SslChannelBindingKey);
            if (result == null)
            {
                // A channel binding object is required.
                throw new InvalidOperationException();
            }

            GlobalLog.Assert(result != null, "GetChannelBindingFromTls returned null even though OS supposedly supports Extended Protection");
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.HttpListener, this, SR.GetString(SR.net_log_listener_cbt));
            }
            return result;
        }

        private bool CheckSpn(NTAuthentication context, bool isSecureConnection, ExtendedProtectionPolicy policy)
        {
            // Kerberos does SPN check already in ASC
            if (context.IsKerberos)
            {
                if (Logging.On)
                {
                    Logging.PrintInfo(Logging.HttpListener, this, SR.GetString(SR.net_log_listener_no_spn_kerberos));
                }
                return true;
            }

            // Don't check the SPN if Extended Protection is off or we already checked the CBT
            if (policy.PolicyEnforcement == PolicyEnforcement.Never)
            {
                if (Logging.On)
                {
                    Logging.PrintInfo(Logging.HttpListener, this, SR.GetString(SR.net_log_listener_no_spn_disabled));
                }
                return true;
            }

            if (ScenarioChecksChannelBinding(isSecureConnection, policy.ProtectionScenario))
            {
                if (Logging.On)
                {
                    Logging.PrintInfo(Logging.HttpListener, this, SR.GetString(SR.net_log_listener_no_spn_cbt));
                }
                return true;
            }

            if (!ExtendedProtectionPolicy.OSSupportsExtendedProtection)
            {
                GlobalLog.Assert(policy.PolicyEnforcement != PolicyEnforcement.Always, "User managed to set PolicyEnforcement.Always when the OS does not support extended protection!");
                if (Logging.On)
                {
                    Logging.PrintInfo(Logging.HttpListener, this, SR.GetString(SR.net_log_listener_no_spn_platform));
                }
                return true;
            }

            string clientSpn = context.ClientSpecifiedSpn;

            // An empty SPN is only allowed in the WhenSupported case
            if (String.IsNullOrEmpty(clientSpn))
            {
                if (policy.PolicyEnforcement == PolicyEnforcement.WhenSupported)
                {
                    if (Logging.On)
                    {
                        Logging.PrintInfo(Logging.HttpListener, this,
                            SR.GetString(SR.net_log_listener_no_spn_whensupported));
                    }
                    return true;
                }
                else
                {
                    if (Logging.On)
                    {
                        Logging.PrintInfo(Logging.HttpListener, this,
                            SR.GetString(SR.net_log_listener_spn_failed_always));
                    }
                    return false;
                }
            }
            else if (String.Compare(clientSpn, "http/localhost", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (Logging.On)
                {
                    Logging.PrintInfo(Logging.HttpListener, this, SR.GetString(SR.net_log_listener_no_spn_loopback));
                }

                return true;
            }
            else
            {
                if (Logging.On)
                {
                    Logging.PrintInfo(Logging.HttpListener, this, SR.GetString(SR.net_log_listener_spn, clientSpn));
                }

                ServiceNameCollection serviceNames = GetServiceNames(policy);

                bool found = serviceNames.Contains(clientSpn);

                if (Logging.On)
                {
                    if (found)
                    {
                        Logging.PrintInfo(Logging.HttpListener, this, SR.GetString(SR.net_log_listener_spn_passed));
                    }
                    else
                    {
                        Logging.PrintInfo(Logging.HttpListener, this, SR.GetString(SR.net_log_listener_spn_failed));

                        if (serviceNames.Count == 0)
                        {
                            Logging.PrintWarning(Logging.HttpListener, this, "CheckSpn",
                                SR.GetString(SR.net_log_listener_spn_failed_empty));
                        }
                        else
                        {
                            Logging.PrintInfo(Logging.HttpListener, this,
                                SR.GetString(SR.net_log_listener_spn_failed_dump));

                            foreach (string serviceName in serviceNames)
                            {
                                Logging.PrintInfo(Logging.HttpListener, this, "\t" + serviceName);
                            }
                        }
                    }
                }

                return found;
            }
        }

        private ServiceNameCollection GetServiceNames(ExtendedProtectionPolicy policy)
        {
            ServiceNameCollection serviceNames;

            if (policy.CustomServiceNames == null)
            {
                if (_defaultServiceNames.ServiceNames.Count == 0)
                {
                    throw new InvalidOperationException(SR.GetString(SR.net_listener_no_spns));
                }
                serviceNames = _defaultServiceNames.ServiceNames;
            }
            else
            {
                serviceNames = policy.CustomServiceNames;
            }
            return serviceNames;
        }

        private ContextFlags GetContextFlags(ExtendedProtectionPolicy policy, bool isSecureConnection)
        {
            ContextFlags result = ContextFlags.Connection;

            if (policy.PolicyEnforcement != PolicyEnforcement.Never)
            {
                if (policy.PolicyEnforcement == PolicyEnforcement.WhenSupported)
                {
                    result |= ContextFlags.AllowMissingBindings;
                }

                if (policy.ProtectionScenario == ProtectionScenario.TrustedProxy)
                {
                    result |= ContextFlags.ProxyBindings;
                }
            }

            return result;
        }

        private static void AddChallenge(ref IList<string> challenges, string challenge)
        {
            if (challenge != null)
            {
                challenge = challenge.Trim();
                if (challenge.Length > 0)
                {
                    GlobalLog.Print("HttpListener:AddChallenge() challenge:" + challenge);
                    if (challenges == null)
                    {
                        challenges = new List<string>(4);
                    }
                    challenges.Add(challenge);
                }
            }
        }

        private IList<string> BuildChallenge(IDictionary<string, object> env, AuthTypes authenticationScheme, out NTAuthentication digestContext,
            ExtendedProtectionPolicy policy)
        {
            GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::BuildChallenge()  authenticationScheme:" + authenticationScheme.ToString());
            IList<string> challenges = null;
            digestContext = null;

            if ((authenticationScheme & AuthTypes.Negotiate) != 0)
            {
                AddChallenge(ref challenges, NegotiationInfoClass.Negotiate);
            }

            if ((authenticationScheme & AuthTypes.Ntlm) != 0)
            {
                AddChallenge(ref challenges, NegotiationInfoClass.NTLM);
            }

            if ((authenticationScheme & AuthTypes.Digest) != 0)
            {
                GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::BuildChallenge() package:WDigest");

                NTAuthentication context = null;
                try
                {
                    bool isSecureConnection = IsSecureConnection(env);
                    string outBlob = null;
                    ChannelBinding binding = GetChannelBinding(env, isSecureConnection, policy);

                    context = new NTAuthentication(true, NegotiationInfoClass.WDigest, null,
                        GetContextFlags(policy, isSecureConnection), binding);

                    SecurityStatus statusCode;
                    outBlob = context.GetOutgoingDigestBlob(null, null, null, Realm, false, false, out statusCode);
                    GlobalLog.Print("HttpListener#" + ValidationHelper.HashString(this) + "::BuildChallenge() GetOutgoingDigestBlob() returned IsCompleted:" + context.IsCompleted + " statusCode:" + statusCode + " outBlob:[" + outBlob + "]");

                    if (context.IsValidContext)
                    {
                        digestContext = context;
                        _digestCache.SaveDigestContext(digestContext);
                    }

                    AddChallenge(ref challenges, NegotiationInfoClass.Digest + (string.IsNullOrEmpty(outBlob) ? string.Empty : " " + outBlob));
                }
                catch (InvalidOperationException)
                {
                    // No CBT available, therefore no digest challenge can be issued.
                }
                finally
                {
                    if (context != null && digestContext != context)
                    {
                        context.CloseContext();
                    }
                }
            }

            return challenges;
        }

        private void RegisterForDisconnectNotification(IDictionary<string, object> env, out DisconnectAsyncResult disconnectResult)
        {
            object connectionId = env[Constants.ServerConnectionIdKey];
            CancellationToken connectionDisconnect = env.Get<CancellationToken>(Constants.ServerConnectionDisconnectKey);
            if (!connectionDisconnect.CanBeCanceled || connectionDisconnect.IsCancellationRequested)
            {
                disconnectResult = null;
                return;
            }
            try
            {
                disconnectResult = new DisconnectAsyncResult(this, connectionId, connectionDisconnect);
            }
            catch (ObjectDisposedException)
            {
                // Just disconnected
                disconnectResult = null;
                return;
            }
        }

        private void SendError(IDictionary<string, object> env, HttpStatusCode httpStatusCode, IList<string> challenges)
        {
            // Send an OWIN HTTP response with the given error status code.
            env[Constants.ResponseStatusCodeKey] = (int)httpStatusCode;

            if (challenges != null)
            {
                var responseHeaders = env.Get<IDictionary<string, string[]>>(Constants.ResponseHeadersKey);
                responseHeaders.Append(HttpKnownHeaderNames.WWWAuthenticate, challenges);
            }
        }

        // This only works for context-destroying errors.
        private HttpStatusCode HttpStatusFromSecurityStatus(SecurityStatus status)
        {
            if (IsCredentialFailure(status))
            {
                return HttpStatusCode.Unauthorized;
            }
            if (IsClientFault(status))
            {
                return HttpStatusCode.BadRequest;
            }
            return HttpStatusCode.InternalServerError;
        }

        // This only works for context-destroying errors.
        private static bool IsCredentialFailure(SecurityStatus error)
        {
            return error == SecurityStatus.LogonDenied ||
                error == SecurityStatus.UnknownCredentials ||
                error == SecurityStatus.NoImpersonation ||
                error == SecurityStatus.NoAuthenticatingAuthority ||
                error == SecurityStatus.UntrustedRoot ||
                error == SecurityStatus.CertExpired ||
                error == SecurityStatus.SmartcardLogonRequired ||
                error == SecurityStatus.BadBinding;
        }

        // This only works for context-destroying errors.
        private static bool IsClientFault(SecurityStatus error)
        {
            return error == SecurityStatus.InvalidToken ||
                error == SecurityStatus.CannotPack ||
                error == SecurityStatus.QopNotSupported ||
                error == SecurityStatus.NoCredentials ||
                error == SecurityStatus.MessageAltered ||
                error == SecurityStatus.OutOfSequence ||
                error == SecurityStatus.IncompleteMessage ||
                error == SecurityStatus.IncompleteCredentials ||
                error == SecurityStatus.WrongPrincipal ||
                error == SecurityStatus.TimeSkew ||
                error == SecurityStatus.IllegalMessage ||
                error == SecurityStatus.CertUnknown ||
                error == SecurityStatus.AlgorithmMismatch ||
                error == SecurityStatus.SecurityQosFailed ||
                error == SecurityStatus.UnsupportedPreauth;
        }
    }
}
