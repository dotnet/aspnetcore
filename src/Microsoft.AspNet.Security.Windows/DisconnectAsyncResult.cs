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
// <copyright file="DisconnectAsyncResult.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Security.Principal;
using System.Threading;

namespace Microsoft.AspNet.Security.Windows
{
    // Keeps NTLM/Negotiate auth contexts alive until the connection is broken.
    internal class DisconnectAsyncResult
    {
        private const string NTLM = "NTLM";

        private object _connectionId;
        private WindowsAuthMiddleware _winAuth;
        private CancellationTokenRegistration _disconnectRegistration;

        private WindowsPrincipal _authenticatedUser;
        private NTAuthentication _session;

        internal DisconnectAsyncResult(WindowsAuthMiddleware winAuth, object connectionId, CancellationToken connectionDisconnect)
        {
            GlobalLog.Print("DisconnectAsyncResult#" + ValidationHelper.HashString(this) + "::.ctor() httpListener#" + ValidationHelper.HashString(winAuth) + " connectionId:" + connectionId);
            _winAuth = winAuth;
            _connectionId = connectionId;
            _winAuth.DisconnectResults[_connectionId] = this;

            // Register with a connection specific CancellationToken.  Without this notification, the contexts will leak indefinitely.
            // Alternatively we could attempt some kind of LRU storage, but this will either have to be larger than your expected connection limit,
            // or will fail at unexpected moments under stress.
            try
            {
                _disconnectRegistration = connectionDisconnect.Register(HandleDisconnect);
            }
            catch (ObjectDisposedException)
            {
                _winAuth.DisconnectResults.Remove(_connectionId);
            }
        }

        internal WindowsPrincipal AuthenticatedUser
        {
            get
            {
                return _authenticatedUser;
            }
            set
            {
                // The previous value can't be disposed because it may be in use by the app.
                _authenticatedUser = value;
            }
        }

        internal NTAuthentication Session
        {
            get
            {
                return _session;
            }
            set
            {
                _session = value;
            }
        }

        private void HandleDisconnect()
        {
            GlobalLog.Print("DisconnectAsyncResult#" + ValidationHelper.HashString(this) + "::HandleDisconnect() DisconnectResults#" + ValidationHelper.HashString(_winAuth.DisconnectResults) + " removing for m_ConnectionId:" + _connectionId);
            _winAuth.DisconnectResults.Remove(_connectionId);
            if (_session != null)
            {
                _session.CloseContext();
            }

            // Clean up the identity. This is for scenarios where identity was not cleaned up before due to
            // identity caching for unsafe ntlm authentication

            IDisposable identity = _authenticatedUser == null ? null : _authenticatedUser.Identity as IDisposable;
            if ((identity != null) &&
                (NTLM.Equals(_authenticatedUser.Identity.AuthenticationType, StringComparison.OrdinalIgnoreCase)) &&
                (_winAuth.UnsafeConnectionNtlmAuthentication))
            {
                identity.Dispose();
            }

            _disconnectRegistration.Dispose();
        }
    }
}
