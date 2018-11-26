// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal class RequestQueue
    {
        private static readonly int BindingInfoSize =
            Marshal.SizeOf<HttpApiTypes.HTTP_BINDING_INFO>();

        private readonly UrlGroup _urlGroup;
        private readonly ILogger _logger;
        private bool _disposed;

        internal RequestQueue(UrlGroup urlGroup, ILogger logger)
        {
            _urlGroup = urlGroup;
            _logger = logger;

            HttpRequestQueueV2Handle requestQueueHandle = null;
            var statusCode = HttpApi.HttpCreateRequestQueue(
                    HttpApi.Version, null, null, 0, out requestQueueHandle);

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
            {
                throw new HttpSysException((int)statusCode);
            }

            // Disabling callbacks when IO operation completes synchronously (returns ErrorCodes.ERROR_SUCCESS)
            if (HttpSysListener.SkipIOCPCallbackOnSuccess &&
                !UnsafeNclNativeMethods.SetFileCompletionNotificationModes(
                    requestQueueHandle,
                    UnsafeNclNativeMethods.FileCompletionNotificationModes.SkipCompletionPortOnSuccess |
                    UnsafeNclNativeMethods.FileCompletionNotificationModes.SkipSetEventOnHandle))
            {
                throw new HttpSysException(Marshal.GetLastWin32Error());
            }

            Handle = requestQueueHandle;
            BoundHandle = ThreadPoolBoundHandle.BindHandle(Handle);
        }

        internal SafeHandle Handle { get; }
        internal ThreadPoolBoundHandle BoundHandle { get; }

        internal unsafe void AttachToUrlGroup()
        {
            CheckDisposed();
            // Set the association between request queue and url group. After this, requests for registered urls will 
            // get delivered to this request queue.

            var info = new HttpApiTypes.HTTP_BINDING_INFO();
            info.Flags = HttpApiTypes.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
            info.RequestQueueHandle = Handle.DangerousGetHandle();

            var infoptr = new IntPtr(&info);

            _urlGroup.SetProperty(HttpApiTypes.HTTP_SERVER_PROPERTY.HttpServerBindingProperty,
                infoptr, (uint)BindingInfoSize);
        }

        internal unsafe void DetachFromUrlGroup()
        {
            CheckDisposed();
            // Break the association between request queue and url group. After this, requests for registered urls 
            // will get 503s.
            // Note that this method may be called multiple times (Stop() and then Abort()). This
            // is fine since http.sys allows to set HttpServerBindingProperty multiple times for valid 
            // Url groups.

            var info = new HttpApiTypes.HTTP_BINDING_INFO();
            info.Flags = HttpApiTypes.HTTP_FLAGS.NONE;
            info.RequestQueueHandle = IntPtr.Zero;

            var infoptr = new IntPtr(&info);

            _urlGroup.SetProperty(HttpApiTypes.HTTP_SERVER_PROPERTY.HttpServerBindingProperty,
                infoptr, (uint)BindingInfoSize, throwOnError: false);
        }

        // The listener must be active for this to work.
        internal unsafe void SetLengthLimit(long length)
        {
            CheckDisposed();

            var result = HttpApi.HttpSetRequestQueueProperty(Handle,
                HttpApiTypes.HTTP_SERVER_PROPERTY.HttpServerQueueLengthProperty,
                new IntPtr((void*)&length), (uint)Marshal.SizeOf<long>(), 0, IntPtr.Zero);

            if (result != 0)
            {
                throw new HttpSysException((int)result);
            }
        }

        // The listener must be active for this to work.
        internal unsafe void SetRejectionVerbosity(Http503VerbosityLevel verbosity)
        {
            CheckDisposed();

            var result = HttpApi.HttpSetRequestQueueProperty(Handle,
                HttpApiTypes.HTTP_SERVER_PROPERTY.HttpServer503VerbosityProperty,
                new IntPtr((void*)&verbosity), (uint)Marshal.SizeOf<long>(), 0, IntPtr.Zero);

            if (result != 0)
            {
                throw new HttpSysException((int)result);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            BoundHandle.Dispose();
            Handle.Dispose();
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
