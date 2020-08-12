// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
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

        private readonly RequestQueueMode _mode;
        private readonly UrlGroup _urlGroup;
        private readonly ILogger _logger;
        private bool _disposed;

        internal RequestQueue(UrlGroup urlGroup, string requestQueueName, RequestQueueMode mode, ILogger logger)
        {
            _mode = mode;
            _urlGroup = urlGroup;
            _logger = logger;

            var flags = HttpApiTypes.HTTP_CREATE_REQUEST_QUEUE_FLAG.None;
            Created = true;
            if (_mode == RequestQueueMode.Attach)
            {
                flags = HttpApiTypes.HTTP_CREATE_REQUEST_QUEUE_FLAG.OpenExisting;
                Created = false;
            }

            var statusCode = HttpApi.HttpCreateRequestQueue(
                    HttpApi.Version,
                    requestQueueName,
                    null,
                    flags,
                    out var requestQueueHandle);

            if (_mode == RequestQueueMode.CreateOrAttach && statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_ALREADY_EXISTS)
            {
                // Tried to create, but it already exists so attach to it instead.
                Created = false;
                flags = HttpApiTypes.HTTP_CREATE_REQUEST_QUEUE_FLAG.OpenExisting;
                statusCode = HttpApi.HttpCreateRequestQueue(
                        HttpApi.Version,
                        requestQueueName,
                        null,
                        flags,
                        out requestQueueHandle);
            }

            if (flags == HttpApiTypes.HTTP_CREATE_REQUEST_QUEUE_FLAG.OpenExisting && statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_FILE_NOT_FOUND)
            {
                throw new HttpSysException((int)statusCode, $"Failed to attach to the given request queue '{requestQueueName}', the queue could not be found.");
            }
            else if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_INVALID_NAME)
            {
                throw new HttpSysException((int)statusCode, $"The given request queue name '{requestQueueName}' is invalid.");
            }
            else if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
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
                requestQueueHandle.Dispose();
                throw new HttpSysException(Marshal.GetLastWin32Error());
            }

            Handle = requestQueueHandle;
            BoundHandle = ThreadPoolBoundHandle.BindHandle(Handle);

            if (!Created)
            {
                _logger.LogInformation(LoggerEventIds.AttachedToQueue, "Attached to an existing request queue '{requestQueueName}', some options do not apply.", requestQueueName);
            }
        }

        /// <summary>
        /// True if this instace created the queue instead of attaching to an existing one.
        /// </summary>
        internal bool Created { get; }

        internal SafeHandle Handle { get; }
        internal ThreadPoolBoundHandle BoundHandle { get; }

        internal unsafe void AttachToUrlGroup()
        {
            Debug.Assert(Created);
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
            Debug.Assert(Created);
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
            Debug.Assert(Created);
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
            Debug.Assert(Created);
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
