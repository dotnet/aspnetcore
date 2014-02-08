//------------------------------------------------------------------------------
// <copyright file="_NTAuthentication.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace Microsoft.AspNet.Security.Windows
{
    internal class NTAuthentication
    {
        private static readonly ContextCallback Callback = new ContextCallback(InitializeCallback);
        private static ISSPIInterface SSPIAuth = new SSPIAuthType();

        private bool _isServer;

        private SafeFreeCredentials _credentialsHandle;
        private SafeDeleteContext _securityContext;
        private string _spn;
        private string _clientSpecifiedSpn;

        private int _tokenSize;
        private ContextFlags _requestedContextFlags;
        private ContextFlags _contextFlags;
        private string _uniqueUserId;

        private bool _isCompleted;
        private string _protocolName;
        private SecSizes _sizes;
        private string _lastProtocolName;
        private string _package;

        private ChannelBinding _channelBinding;

        // This overload does not attmept to impersonate because the caller either did it already or the original thread context is still preserved

        internal NTAuthentication(bool isServer, string package, NetworkCredential credential, string spn, ContextFlags requestedContextFlags, ChannelBinding channelBinding)
        {
            Initialize(isServer, package, credential, spn, requestedContextFlags, channelBinding);
        }

        // This overload always uses the default credentials for the process.

        [SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.ControlPrincipal)]
        internal NTAuthentication(bool isServer, string package, string spn, ContextFlags requestedContextFlags, ChannelBinding channelBinding)
        {
            try
            {
                using (WindowsIdentity.Impersonate(IntPtr.Zero))
                {
                    Initialize(isServer, package, CredentialCache.DefaultNetworkCredentials, spn, requestedContextFlags, channelBinding);
                }
            }
            catch
            {
                // Avoid exception filter attacks.
                throw;
            }
        }

        // The semantic of this propoerty is "Don't call me again".
        // It can be completed either with success or error
        // The latest case is signalled by IsValidContext==false
        internal bool IsCompleted
        {
            get
            {
                return _isCompleted;
            }
        }

        internal bool IsValidContext
        {
            get
            {
                return !(_securityContext == null || _securityContext.IsInvalid);
            }
        }

        internal string AssociatedName
        {
            get
            {
                if (!(IsValidContext && IsCompleted))
                {
                    throw new Win32Exception((int)SecurityStatus.InvalidHandle);
                }

                string name = SSPIWrapper.QueryContextAttributes(SSPIAuth, _securityContext, ContextAttribute.Names) as string;
                GlobalLog.Print("NTAuthentication: The context is associated with [" + name + "]");
                return name;
            }
        }

        internal bool IsConfidentialityFlag
        {
            get
            {
                return (_contextFlags & ContextFlags.Confidentiality) != 0;
            }
        }

        internal bool IsIntegrityFlag
        {
            get
            {
                return (_contextFlags & (_isServer ? ContextFlags.AcceptIntegrity : ContextFlags.InitIntegrity)) != 0;
            }
        }

        internal bool IsMutualAuthFlag
        {
            get
            {
                return (_contextFlags & ContextFlags.MutualAuth) != 0;
            }
        }

        internal bool IsDelegationFlag
        {
            get
            {
                return (_contextFlags & ContextFlags.Delegate) != 0;
            }
        }

        internal bool IsIdentifyFlag
        {
            get
            {
                return (_contextFlags & (_isServer ? ContextFlags.AcceptIdentify : ContextFlags.InitIdentify)) != 0;
            }
        }

        internal string Spn
        {
            get
            {
                return _spn;
            }
        }

        internal string ClientSpecifiedSpn
        {
            get
            {
                if (_clientSpecifiedSpn == null)
                {
                    _clientSpecifiedSpn = GetClientSpecifiedSpn();
                }
                return _clientSpecifiedSpn;
            }
        }

        // True indicates this instance is for Server and will use AcceptSecurityContext SSPI API

        internal bool IsServer
        {
            get
            {
                return _isServer;
            }
        }

        internal bool IsKerberos
        {
            get
            {
                if (_lastProtocolName == null)
                {
                    _lastProtocolName = ProtocolName;
                }

                return (object)_lastProtocolName == (object)NegotiationInfoClass.Kerberos;
            }
        }
        internal bool IsNTLM
        {
            get
            {
                if (_lastProtocolName == null)
                {
                    _lastProtocolName = ProtocolName;
                }

                return (object)_lastProtocolName == (object)NegotiationInfoClass.NTLM;
            }
        }

        internal string Package
        {
            get
            {
                return _package;
            }
        }

        internal string ProtocolName
        {
            get
            {
                // NB: May return string.Empty if the auth is not done yet or failed
                if (_protocolName == null)
                {
                    NegotiationInfoClass negotiationInfo = null;

                    if (IsValidContext)
                    {
                        negotiationInfo = SSPIWrapper.QueryContextAttributes(SSPIAuth, _securityContext, ContextAttribute.NegotiationInfo) as NegotiationInfoClass;
                        if (IsCompleted)
                        {
                            if (negotiationInfo != null)
                            {
                                // cache it only when it's completed
                                _protocolName = negotiationInfo.AuthenticationPackage;
                            }
                        }
                    }
                    return negotiationInfo == null ? string.Empty : negotiationInfo.AuthenticationPackage;
                }
                return _protocolName;
            }
        }

        internal SecSizes Sizes
        {
            get
            {
                GlobalLog.Assert(IsCompleted && IsValidContext, "NTAuthentication#{0}::MaxDataSize|The context is not completed or invalid.", ValidationHelper.HashString(this));
                if (_sizes == null)
                {
                    _sizes = SSPIWrapper.QueryContextAttributes(
                                  SSPIAuth,
                                  _securityContext,
                                  ContextAttribute.Sizes) as SecSizes;
                }
                return _sizes;
            }
        }

        internal ChannelBinding ChannelBinding
        {
            get { return _channelBinding; }
        }

        private static void InitializeCallback(object state)
        {
            InitializeCallbackContext context = (InitializeCallbackContext)state;
            context.ThisPtr.Initialize(context.IsServer, context.Package, context.Credential, context.Spn, context.RequestedContextFlags, context.ChannelBinding);
        }

        private void Initialize(bool isServer, string package, NetworkCredential credential, string spn, ContextFlags requestedContextFlags, ChannelBinding channelBinding)
        {
            GlobalLog.Print("NTAuthentication#" + ValidationHelper.HashString(this) + "::.ctor() package:" + ValidationHelper.ToString(package) + " spn:" + ValidationHelper.ToString(spn) + " flags :" + requestedContextFlags.ToString());
            _tokenSize = SSPIWrapper.GetVerifyPackageInfo(SSPIAuth, package, true).MaxToken;
            _isServer = isServer;
            _spn = spn;
            _securityContext = null;
            _requestedContextFlags = requestedContextFlags;
            _package = package;
            _channelBinding = channelBinding;

            GlobalLog.Print("Peer SPN-> '" + _spn + "'");

            // check if we're using DefaultCredentials

            if (credential == CredentialCache.DefaultNetworkCredentials)
            {
                GlobalLog.Print("NTAuthentication#" + ValidationHelper.HashString(this) + "::.ctor(): using DefaultCredentials");
                _credentialsHandle = SSPIWrapper.AcquireDefaultCredential(
                                                    SSPIAuth,
                                                    package,
                                                    (_isServer ? CredentialUse.Inbound : CredentialUse.Outbound));
                _uniqueUserId = "/S"; // save off for unique connection marking ONLY used by HTTP client
            }
            else if (ComNetOS.IsWin7orLater)
            {
                unsafe
                {
                    SafeSspiAuthDataHandle authData = null;
                    try
                    {
                        SecurityStatus result = UnsafeNclNativeMethods.SspiHelper.SspiEncodeStringsAsAuthIdentity(
                            credential.UserName/*InternalGetUserName()*/, credential.Domain/*InternalGetDomain()*/,
                            credential.Password/*InternalGetPassword()*/, out authData);

                        if (result != SecurityStatus.OK)
                        {
                            if (Logging.On)
                            {
                                Logging.PrintError(Logging.Web, SR.GetString(SR.net_log_operation_failed_with_error, "SspiEncodeStringsAsAuthIdentity()", String.Format(CultureInfo.CurrentCulture, "0x{0:X}", (int)result)));
                            }
                            throw new Win32Exception((int)result);
                        }

                        _credentialsHandle = SSPIWrapper.AcquireCredentialsHandle(SSPIAuth,
                            package, (_isServer ? CredentialUse.Inbound : CredentialUse.Outbound), ref authData);
                    }
                    finally
                    {
                        if (authData != null)
                        {
                            authData.Dispose();
                        }
                    }
                }
            }
            else
            {
                // we're not using DefaultCredentials, we need a
                // AuthIdentity struct to contain credentials
                // SECREVIEW:
                // we'll save username/domain in temp strings, to avoid decrypting multiple times.
                // password is only used once

                string username = credential.UserName; // InternalGetUserName();

                string domain = credential.Domain; // InternalGetDomain();
                // ATTN:
                // NetworkCredential class does not differentiate between null and "" but SSPI packages treat these cases differently
                // For NTLM we want to keep "" for Wdigest.Dll we should use null.
                AuthIdentity authIdentity = new AuthIdentity(username, credential.Password/*InternalGetPassword()*/, (object)package == (object)NegotiationInfoClass.WDigest && (domain == null || domain.Length == 0) ? null : domain);

                _uniqueUserId = domain + "/" + username + "/U"; // save off for unique connection marking ONLY used by HTTP client

                GlobalLog.Print("NTAuthentication#" + ValidationHelper.HashString(this) + "::.ctor(): using authIdentity:" + authIdentity.ToString());

                _credentialsHandle = SSPIWrapper.AcquireCredentialsHandle(
                                                    SSPIAuth,
                                                    package,
                                                    (_isServer ? CredentialUse.Inbound : CredentialUse.Outbound),
                                                    ref authIdentity);
            }
        }
        
        // This will return an client token when conducted authentication on server side'
        // This token can be used ofr impersanation
        // We use it to create a WindowsIdentity and hand it out to the server app.
        internal SafeCloseHandle GetContextToken(out SecurityStatus status)
        {
            GlobalLog.Assert(IsCompleted && IsValidContext, "NTAuthentication#{0}::GetContextToken|Should be called only when completed with success, currently is not!", ValidationHelper.HashString(this));
            GlobalLog.Assert(IsServer, "NTAuthentication#{0}::GetContextToken|The method must not be called by the client side!", ValidationHelper.HashString(this));

            if (!IsValidContext)
            {
                throw new Win32Exception((int)SecurityStatus.InvalidHandle);
            }

            SafeCloseHandle token = null;
            status = (SecurityStatus)SSPIWrapper.QuerySecurityContextToken(
                SSPIAuth,
                _securityContext,
                out token);

            return token;
        }

        internal SafeCloseHandle GetContextToken()
        {
            SecurityStatus status;
            SafeCloseHandle token = GetContextToken(out status);
            if (status != SecurityStatus.OK)
            {
                throw new Win32Exception((int)status);
            }
            return token;
        }

        internal void CloseContext()
        {
            if (_securityContext != null && !_securityContext.IsClosed)
                {
                    _securityContext.Dispose();
                }
        }

        // NTAuth::GetOutgoingBlob()
        // Created:   12-01-1999: L.M.
        // Description:
        // Accepts an incoming binary security blob  and returns
        // an outgoing binary security blob
        internal byte[] GetOutgoingBlob(byte[] incomingBlob, bool throwOnError, out SecurityStatus statusCode)
        {
            GlobalLog.Enter("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingBlob", ((incomingBlob == null) ? "0" : incomingBlob.Length.ToString(NumberFormatInfo.InvariantInfo)) + " bytes");

            List<SecurityBuffer> list = new List<SecurityBuffer>(2);

            if (incomingBlob != null)
            {
                list.Add(new SecurityBuffer(incomingBlob, BufferType.Token));
            }
            if (_channelBinding != null)
            {
                list.Add(new SecurityBuffer(_channelBinding));
            }

            SecurityBuffer[] inSecurityBufferArray = null;
            if (list.Count > 0)
            {
                inSecurityBufferArray = list.ToArray();
            }

            SecurityBuffer outSecurityBuffer = new SecurityBuffer(_tokenSize, BufferType.Token);

            bool firstTime = _securityContext == null;
            try
            {
                if (!_isServer)
                {
                    // client session
                    statusCode = (SecurityStatus)SSPIWrapper.InitializeSecurityContext(
                        SSPIAuth,
                        _credentialsHandle,
                        ref _securityContext,
                        _spn,
                        _requestedContextFlags,
                        Endianness.Network,
                        inSecurityBufferArray,
                        outSecurityBuffer,
                        ref _contextFlags);

                    GlobalLog.Print("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingBlob() SSPIWrapper.InitializeSecurityContext() returns statusCode:0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");

                    if (statusCode == SecurityStatus.CompleteNeeded)
                    {
                        SecurityBuffer[] inSecurityBuffers = new SecurityBuffer[1];
                        inSecurityBuffers[0] = outSecurityBuffer;

                        statusCode = (SecurityStatus)SSPIWrapper.CompleteAuthToken(
                            SSPIAuth,
                            ref _securityContext,
                            inSecurityBuffers);

                        GlobalLog.Print("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingDigestBlob() SSPIWrapper.CompleteAuthToken() returns statusCode:0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
                        outSecurityBuffer.token = null;
                    }
                }
                else
                {
                    // server session
                    statusCode = (SecurityStatus)SSPIWrapper.AcceptSecurityContext(
                        SSPIAuth,
                        _credentialsHandle,
                        ref _securityContext,
                        _requestedContextFlags,
                        Endianness.Network,
                        inSecurityBufferArray,
                        outSecurityBuffer,
                        ref _contextFlags);

                    GlobalLog.Print("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingBlob() SSPIWrapper.AcceptSecurityContext() returns statusCode:0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
                }
            }
            finally
            {
                // Assuming the ISC or ASC has referenced the credential on the first successful call,
                // we want to decrement the effective ref count by "disposing" it.
                // The real dispose will happen when the security context is closed.
                // Note if the first call was not successfull the handle is physically destroyed here

                if (firstTime && _credentialsHandle != null)
                    {
                        _credentialsHandle.Dispose();
                    }
            }

            if (((int)statusCode & unchecked((int)0x80000000)) != 0)
            {
                CloseContext();
                _isCompleted = true;
                if (throwOnError)
                {
                    Win32Exception exception = new Win32Exception((int)statusCode);
                    GlobalLog.Leave("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingBlob", "Win32Exception:" + exception);
                    throw exception;
                }
                GlobalLog.Leave("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingBlob", "null statusCode:0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
                return null;
            }
            else if (firstTime && _credentialsHandle != null)
            {
                // cache until it is pushed out by newly incoming handles
                SSPIHandleCache.CacheCredential(_credentialsHandle);
            }

            // the return value from SSPI will tell us correctly if the
            // handshake is over or not: http://msdn.microsoft.com/library/psdk/secspi/sspiref_67p0.htm
            // we also have to consider the case in which SSPI formed a new context, in this case we're done as well.
            if (statusCode == SecurityStatus.OK)
            {
                // we're sucessfully done
                GlobalLog.Assert(statusCode == SecurityStatus.OK, "NTAuthentication#{0}::GetOutgoingBlob()|statusCode:[0x{1:x8}] ({2}) m_SecurityContext#{3}::Handle:[{4}] [STATUS != OK]", ValidationHelper.HashString(this), (int)statusCode, statusCode, ValidationHelper.HashString(_securityContext), ValidationHelper.ToString(_securityContext));
                _isCompleted = true;
            }
            else
            {
                // we need to continue
                GlobalLog.Print("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingBlob() need continue statusCode:[0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + "] (" + statusCode.ToString() + ") m_SecurityContext#" + ValidationHelper.HashString(_securityContext) + "::Handle:" + ValidationHelper.ToString(_securityContext) + "]");
            }
            // GlobalLog.Print("out token = " + outSecurityBuffer.ToString());
            //            GlobalLog.Dump(outSecurityBuffer.token);
            GlobalLog.Leave("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingBlob", "IsCompleted:" + IsCompleted.ToString());
            return outSecurityBuffer.token;
        }

        // for Server side (IIS 6.0) see: \\netindex\Sources\inetsrv\iis\iisrearc\iisplus\ulw3\digestprovider.cxx
        // for Client side (HTTP.SYS) see: \\netindex\Sources\net\http\sys\ucauth.c
        internal string GetOutgoingDigestBlob(string incomingBlob, string requestMethod, string requestedUri, string realm, bool isClientPreAuth, bool throwOnError, out SecurityStatus statusCode)
        {
            GlobalLog.Enter("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingDigestBlob", incomingBlob);

            // second time call with 3 incoming buffers to select HTTP client.
            // we should get back a SecurityStatus.OK and a non null outgoingBlob.
            SecurityBuffer[] inSecurityBuffers = null;
            SecurityBuffer outSecurityBuffer = new SecurityBuffer(_tokenSize, isClientPreAuth ? BufferType.Parameters : BufferType.Token);

            bool firstTime = _securityContext == null;
            try
            {
                if (!_isServer)
                {
                    // client session

                    if (!isClientPreAuth)
                    {
                        if (incomingBlob != null)
                        {
                            List<SecurityBuffer> list = new List<SecurityBuffer>(5);

                            list.Add(new SecurityBuffer(HeaderEncoding.GetBytes(incomingBlob), BufferType.Token));
                            list.Add(new SecurityBuffer(HeaderEncoding.GetBytes(requestMethod), BufferType.Parameters));
                            list.Add(new SecurityBuffer(null, BufferType.Parameters));
                            list.Add(new SecurityBuffer(Encoding.Unicode.GetBytes(_spn), BufferType.TargetHost));

                            if (_channelBinding != null)
                            {
                                list.Add(new SecurityBuffer(_channelBinding));
                            }

                            inSecurityBuffers = list.ToArray();
                        }

                        statusCode = (SecurityStatus)SSPIWrapper.InitializeSecurityContext(
                            SSPIAuth,
                            _credentialsHandle,
                            ref _securityContext,
                            requestedUri, // this must match the Uri in the HTTP status line for the current request
                            _requestedContextFlags,
                            Endianness.Network,
                            inSecurityBuffers,
                            outSecurityBuffer,
                            ref _contextFlags);

                        GlobalLog.Print("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingDigestBlob() SSPIWrapper.InitializeSecurityContext() returns statusCode:0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
                    }
                    else
                    {
#if WDIGEST_PREAUTH
                        inSecurityBuffers = new SecurityBuffer[] {
                            new SecurityBuffer(null, BufferType.Token),
                            new SecurityBuffer(WebHeaderCollection.HeaderEncoding.GetBytes(requestMethod), BufferType.Parameters),
                            new SecurityBuffer(WebHeaderCollection.HeaderEncoding.GetBytes(requestedUri), BufferType.Parameters),
                            new SecurityBuffer(null, BufferType.Parameters),
                            outSecurityBuffer,
                        };

                        statusCode = (SecurityStatus) SSPIWrapper.MakeSignature(GlobalSSPI.SSPIAuth, m_SecurityContext, inSecurityBuffers, 0);

                        GlobalLog.Print("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingDigestBlob() SSPIWrapper.MakeSignature() returns statusCode:0x" + ((int) statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
#else
                        statusCode = SecurityStatus.OK;
                        GlobalLog.Assert("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingDigestBlob()", "Invalid code path.");
#endif
                    }
                }
                else
                {
                    // server session
                    List<SecurityBuffer> list = new List<SecurityBuffer>(6);

                    list.Add(incomingBlob == null ? new SecurityBuffer(0, BufferType.Token) : new SecurityBuffer(HeaderEncoding.GetBytes(incomingBlob), BufferType.Token));
                    list.Add(requestMethod == null ? new SecurityBuffer(0, BufferType.Parameters) : new SecurityBuffer(HeaderEncoding.GetBytes(requestMethod), BufferType.Parameters));
                    list.Add(requestedUri == null ? new SecurityBuffer(0, BufferType.Parameters) : new SecurityBuffer(HeaderEncoding.GetBytes(requestedUri), BufferType.Parameters));
                    list.Add(new SecurityBuffer(0, BufferType.Parameters));
                    list.Add(realm == null ? new SecurityBuffer(0, BufferType.Parameters) : new SecurityBuffer(Encoding.Unicode.GetBytes(realm), BufferType.Parameters));

                    if (_channelBinding != null)
                    {
                        list.Add(new SecurityBuffer(_channelBinding));
                    }

                    inSecurityBuffers = list.ToArray();

                    statusCode = (SecurityStatus)SSPIWrapper.AcceptSecurityContext(
                        SSPIAuth,
                        _credentialsHandle,
                        ref _securityContext,
                        _requestedContextFlags,
                        Endianness.Network,
                        inSecurityBuffers,
                        outSecurityBuffer,
                        ref _contextFlags);

                    GlobalLog.Print("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingDigestBlob() SSPIWrapper.AcceptSecurityContext() returns statusCode:0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");

                    if (statusCode == SecurityStatus.CompleteNeeded)
                    {
                        inSecurityBuffers[4] = outSecurityBuffer;

                        statusCode = (SecurityStatus)SSPIWrapper.CompleteAuthToken(
                                SSPIAuth,
                                ref _securityContext,
                                inSecurityBuffers);

                        GlobalLog.Print("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingDigestBlob() SSPIWrapper.CompleteAuthToken() returns statusCode:0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");

                        outSecurityBuffer.token = null;
                    }
                }
            }
            finally
            {
                // Assuming the ISC or ASC has referenced the credential on the first successful call,
                // we want to decrement the effective ref count by "disposing" it.
                // The real dispose will happen when the security context is closed.
                // Note if the first call was not successfull the handle is physically destroyed here

                if (firstTime && _credentialsHandle != null)
                    {
                        _credentialsHandle.Dispose();
                    }
            }

            if (((int)statusCode & unchecked((int)0x80000000)) != 0)
            {
                CloseContext();
                if (throwOnError)
                {
                    Win32Exception exception = new Win32Exception((int)statusCode);
                    GlobalLog.Leave("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingDigestBlob", "Win32Exception:" + exception);
                    throw exception;
                }
                GlobalLog.Leave("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingDigestBlob", "null statusCode:0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
                return null;
            }
            else if (firstTime && _credentialsHandle != null)
            {
                // cache until it is pushed out by newly incoming handles
                SSPIHandleCache.CacheCredential(_credentialsHandle);
            }

            // the return value from SSPI will tell us correctly if the
            // handshake is over or not: http://msdn.microsoft.com/library/psdk/secspi/sspiref_67p0.htm
            if (statusCode == SecurityStatus.OK)
            {
                // we're done, cleanup
                _isCompleted = true;
            }
            else
            {
                // we need to continue
                GlobalLog.Print("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingDigestBlob() need continue statusCode:[0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + "] (" + statusCode.ToString() + ") m_SecurityContext#" + ValidationHelper.HashString(_securityContext) + "::Handle:" + ValidationHelper.ToString(_securityContext) + "]");
            }
            GlobalLog.Print("out token = " + outSecurityBuffer.ToString());
            GlobalLog.Dump(outSecurityBuffer.token);
            GlobalLog.Print("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingDigestBlob() IsCompleted:" + IsCompleted.ToString());

            byte[] decodedOutgoingBlob = outSecurityBuffer.token;
            string outgoingBlob = null;
            if (decodedOutgoingBlob != null && decodedOutgoingBlob.Length > 0)
            {
                outgoingBlob = HeaderEncoding.GetString(decodedOutgoingBlob, 0, outSecurityBuffer.size);
            }
            GlobalLog.Leave("NTAuthentication#" + ValidationHelper.HashString(this) + "::GetOutgoingDigestBlob", outgoingBlob);
            return outgoingBlob;
        }

        private string GetClientSpecifiedSpn()
        {
            GlobalLog.Assert(IsValidContext && IsCompleted, "NTAuthentication: Trying to get the client SPN before handshaking is done!");

            string spn = SSPIWrapper.QueryContextAttributes(SSPIAuth, _securityContext,
                ContextAttribute.ClientSpecifiedSpn) as string;

            GlobalLog.Print("NTAuthentication: The client specified SPN is [" + spn + "]");
            return spn;
        }

        private class InitializeCallbackContext
        {
            internal readonly NTAuthentication ThisPtr;
            internal readonly bool IsServer;
            internal readonly string Package;
            internal readonly NetworkCredential Credential;
            internal readonly string Spn;
            internal readonly ContextFlags RequestedContextFlags;
            internal readonly ChannelBinding ChannelBinding;

            internal InitializeCallbackContext(NTAuthentication thisPtr, bool isServer, string package, NetworkCredential credential, string spn, ContextFlags requestedContextFlags, ChannelBinding channelBinding)
            {
                this.ThisPtr = thisPtr;
                this.IsServer = isServer;
                this.Package = package;
                this.Credential = credential;
                this.Spn = spn;
                this.RequestedContextFlags = requestedContextFlags;
                this.ChannelBinding = channelBinding;
            }
        }
    }
}
