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
// <copyright file="TimeoutManager.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Net.Http.Server
{
    // See the native HTTP_TIMEOUT_LIMIT_INFO structure documentation for additional information.
    // http://msdn.microsoft.com/en-us/library/aa364661.aspx

    /// <summary>
    /// Exposes the Http.Sys timeout configurations.  These may also be configured in the registry.
    /// </summary>
    public sealed class TimeoutManager
    {
        private static readonly int TimeoutLimitSize =
            Marshal.SizeOf<HttpApi.HTTP_TIMEOUT_LIMIT_INFO>();

        private WebListener _server;
        private int[] _timeouts;
        private uint _minSendBytesPerSecond;

        internal TimeoutManager(WebListener listener)
        {
            _server = listener;

            // We have to maintain local state since we allow applications to set individual timeouts. Native Http
            // API for setting timeouts expects all timeout values in every call so we have remember timeout values 
            // to fill in the blanks. Except MinSendBytesPerSecond, local state for remaining five timeouts is 
            // maintained in timeouts array.
            //
            // No initialization is required because a value of zero indicates that system defaults should be used.
            _timeouts = new int[5];
        }

        #region Properties

        /// <summary>
        /// The time, in seconds, allowed for the request entity body to arrive.  The default timer is 2 minutes.
        /// 
        /// The HTTP Server API turns on this timer when the request has an entity body. The timer expiration is 
        /// initially set to the configured value. When the HTTP Server API receives additional data indications on the 
        /// request, it resets the timer to give the connection another interval.
        ///
        /// Use TimeSpan.Zero to indicate that system defaults should be used.
        /// </summary>
        public TimeSpan EntityBody
        {
            get
            {
                return GetTimeSpanTimeout(HttpApi.HTTP_TIMEOUT_TYPE.EntityBody);
            }
            set
            {
                SetTimeSpanTimeout(HttpApi.HTTP_TIMEOUT_TYPE.EntityBody, value);
            }
        }

        /// <summary>
        /// The time, in seconds, allowed for the HTTP Server API to drain the entity body on a Keep-Alive connection. 
        /// The default timer is 2 minutes.
        /// 
        /// On a Keep-Alive connection, after the application has sent a response for a request and before the request 
        /// entity body has completely arrived, the HTTP Server API starts draining the remainder of the entity body to 
        /// reach another potentially pipelined request from the client. If the time to drain the remaining entity body 
        /// exceeds the allowed period the connection is timed out.
        ///
        /// Use TimeSpan.Zero to indicate that system defaults should be used.
        /// </summary>
        public TimeSpan DrainEntityBody
        {
            get
            {
                return GetTimeSpanTimeout(HttpApi.HTTP_TIMEOUT_TYPE.DrainEntityBody);
            }
            set
            {
                SetTimeSpanTimeout(HttpApi.HTTP_TIMEOUT_TYPE.DrainEntityBody, value);
            }
        }

        /// <summary>
        /// The time, in seconds, allowed for the request to remain in the request queue before the application picks 
        /// it up.  The default timer is 2 minutes.
        ///
        /// Use TimeSpan.Zero to indicate that system defaults should be used.
        /// </summary>
        public TimeSpan RequestQueue
        {
            get
            {
                return GetTimeSpanTimeout(HttpApi.HTTP_TIMEOUT_TYPE.RequestQueue);
            }
            set
            {
                SetTimeSpanTimeout(HttpApi.HTTP_TIMEOUT_TYPE.RequestQueue, value);
            }
        }

        /// <summary>
        /// The time, in seconds, allowed for an idle connection.  The default timer is 2 minutes.
        /// 
        /// This timeout is only enforced after the first request on the connection is routed to the application.
        ///
        /// Use TimeSpan.Zero to indicate that system defaults should be used.
        /// </summary>
        public TimeSpan IdleConnection
        {
            get
            {
                return GetTimeSpanTimeout(HttpApi.HTTP_TIMEOUT_TYPE.IdleConnection);
            }
            set
            {
                SetTimeSpanTimeout(HttpApi.HTTP_TIMEOUT_TYPE.IdleConnection, value);
            }
        }

        /// <summary>
        /// The time, in seconds, allowed for the HTTP Server API to parse the request header.  The default timer is 
        /// 2 minutes.
        ///  
        /// This timeout is only enforced after the first request on the connection is routed to the application.
        ///
        /// Use TimeSpan.Zero to indicate that system defaults should be used.
        /// </summary>
        public TimeSpan HeaderWait
        {
            get
            {
                return GetTimeSpanTimeout(HttpApi.HTTP_TIMEOUT_TYPE.HeaderWait);
            }
            set
            {
                SetTimeSpanTimeout(HttpApi.HTTP_TIMEOUT_TYPE.HeaderWait, value);
            }
        }

        /// <summary>
        /// The minimum send rate, in bytes-per-second, for the response. The default response send rate is 150 
        /// bytes-per-second.
        /// 
        /// Use 0 to indicate that system defaults should be used.
        ///
        /// To disable this timer set it to UInt32.MaxValue
        /// </summary>
        public long MinSendBytesPerSecond
        {
            get
            {
                // Since we maintain local state, GET is local.
                return _minSendBytesPerSecond;
            }
            set
            {
                // MinSendRate value is ULONG in native layer.
                if (value < 0 || value > uint.MaxValue)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                SetServerTimeouts(_timeouts, (uint)value);
                _minSendBytesPerSecond = (uint)value;
            }
        }

        #endregion Properties

        #region Helpers

        private TimeSpan GetTimeSpanTimeout(HttpApi.HTTP_TIMEOUT_TYPE type)
        {
            // Since we maintain local state, GET is local.
            return new TimeSpan(0, 0, (int)_timeouts[(int)type]);
        }

        private void SetTimeSpanTimeout(HttpApi.HTTP_TIMEOUT_TYPE type, TimeSpan value)
        {
            // All timeouts are defined as USHORT in native layer (except MinSendRate, which is ULONG). Make sure that
            // timeout value is within range.

            var timeoutValue = Convert.ToInt64(value.TotalSeconds);

            if (timeoutValue < 0 || timeoutValue > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException("value");
            }

            // Use local state to get values for other timeouts. Call into the native layer and if that 
            // call succeeds, update local state.
            var newTimeouts = (int[])_timeouts.Clone();
            newTimeouts[(int)type] = (int)timeoutValue;
            SetServerTimeouts(newTimeouts, _minSendBytesPerSecond);
            _timeouts[(int)type] = (int)timeoutValue;
        }

        private unsafe void SetServerTimeouts(int[] timeouts, uint minSendBytesPerSecond)
        {
            var timeoutinfo = new HttpApi.HTTP_TIMEOUT_LIMIT_INFO();

            timeoutinfo.Flags = HttpApi.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
            timeoutinfo.DrainEntityBody =
                (ushort)timeouts[(int)HttpApi.HTTP_TIMEOUT_TYPE.DrainEntityBody];
            timeoutinfo.EntityBody =
                (ushort)timeouts[(int)HttpApi.HTTP_TIMEOUT_TYPE.EntityBody];
            timeoutinfo.RequestQueue =
                (ushort)timeouts[(int)HttpApi.HTTP_TIMEOUT_TYPE.RequestQueue];
            timeoutinfo.IdleConnection =
                (ushort)timeouts[(int)HttpApi.HTTP_TIMEOUT_TYPE.IdleConnection];
            timeoutinfo.HeaderWait =
                (ushort)timeouts[(int)HttpApi.HTTP_TIMEOUT_TYPE.HeaderWait];
            timeoutinfo.MinSendRate = minSendBytesPerSecond;

            var infoptr = new IntPtr(&timeoutinfo);

            _server.UrlGroup.SetProperty(
                HttpApi.HTTP_SERVER_PROPERTY.HttpServerTimeoutsProperty,
                infoptr, (uint)TimeoutLimitSize);
        }

        #endregion Helpers
    }
}
