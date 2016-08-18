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

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.Net.Http.Server
{
    internal class UrlGroup : IDisposable
    {
        private ServerSession _serverSession;
        private ILogger _logger;
        private bool _disposed;

        internal unsafe UrlGroup(ServerSession serverSession, ILogger logger)
        {
            _serverSession = serverSession;
            _logger = logger;

            ulong urlGroupId = 0;
            var statusCode = HttpApi.HttpCreateUrlGroup(
                _serverSession.Id.DangerousGetServerSessionId(), &urlGroupId, 0);

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
            {
                throw new WebListenerException((int)statusCode);
            }

            Debug.Assert(urlGroupId != 0, "Invalid id returned by HttpCreateUrlGroup");
            Id = urlGroupId;
        }

        internal ulong Id { get; private set; }

        internal void SetProperty(HttpApi.HTTP_SERVER_PROPERTY property, IntPtr info, uint infosize, bool throwOnError = true)
        {            
            Debug.Assert(info != IntPtr.Zero, "SetUrlGroupProperty called with invalid pointer");
            CheckDisposed();

            var statusCode = HttpApi.HttpSetUrlGroupProperty(Id, property, info, infosize);

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
            {
                var exception = new WebListenerException((int)statusCode);
                LogHelper.LogException(_logger, "SetUrlGroupProperty", exception);
                if (throwOnError)
                {
                    throw exception;
                }
            }
        }

        internal void RegisterPrefix(string uriPrefix, int contextId)
        {
            LogHelper.LogInfo(_logger, "Listening on prefix: " + uriPrefix);
            CheckDisposed();

            var statusCode = HttpApi.HttpAddUrlToUrlGroup(Id, uriPrefix, (ulong)contextId, 0);

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
            {
                if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_ALREADY_EXISTS)
                {
                    throw new WebListenerException((int)statusCode, string.Format(Resources.Exception_PrefixAlreadyRegistered, uriPrefix));
                }
                else
                {
                    throw new WebListenerException((int)statusCode);
                }
            }
        }
        
        internal bool UnregisterPrefix(string uriPrefix)
        {
            LogHelper.LogInfo(_logger, "Stop listening on prefix: " + uriPrefix);
            CheckDisposed();

            var statusCode = HttpApi.HttpRemoveUrlFromUrlGroup(Id, uriPrefix, 0);

            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_NOT_FOUND)
            {
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            Debug.Assert(Id != 0, "HttpCloseUrlGroup called with invalid url group id");

            uint statusCode = HttpApi.HttpCloseUrlGroup(Id);

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
            {
                LogHelper.LogError(_logger, "CleanupV2Config", "Result: " + statusCode);
            }
            Id = 0;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}
