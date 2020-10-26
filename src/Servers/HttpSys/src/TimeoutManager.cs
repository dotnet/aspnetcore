// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.HttpSys.Internal;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    // See the native HTTP_TIMEOUT_LIMIT_INFO structure documentation for additional information.
    // http://msdn.microsoft.com/en-us/library/aa364661.aspx

    /// <summary>
    /// Exposes the Http.Sys timeout configurations.  These may also be configured in the registry.
    /// These settings do not apply when attaching to an existing queue.
    /// </summary>
    public sealed class TimeoutManager
    {
        private static readonly int TimeoutLimitSize =
            Marshal.SizeOf<HttpApiTypes.HTTP_TIMEOUT_LIMIT_INFO>();

        private UrlGroup _urlGroup;
        private int[] _timeouts;
        private uint _minSendBytesPerSecond;

        internal TimeoutManager()
        {
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
                return GetTimeSpanTimeout(HttpApiTypes.HTTP_TIMEOUT_TYPE.EntityBody);
            }
            set
            {
                SetTimeSpanTimeout(HttpApiTypes.HTTP_TIMEOUT_TYPE.EntityBody, value);
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
                return GetTimeSpanTimeout(HttpApiTypes.HTTP_TIMEOUT_TYPE.DrainEntityBody);
            }
            set
            {
                SetTimeSpanTimeout(HttpApiTypes.HTTP_TIMEOUT_TYPE.DrainEntityBody, value);
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
                return GetTimeSpanTimeout(HttpApiTypes.HTTP_TIMEOUT_TYPE.RequestQueue);
            }
            set
            {
                SetTimeSpanTimeout(HttpApiTypes.HTTP_TIMEOUT_TYPE.RequestQueue, value);
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
                return GetTimeSpanTimeout(HttpApiTypes.HTTP_TIMEOUT_TYPE.IdleConnection);
            }
            set
            {
                SetTimeSpanTimeout(HttpApiTypes.HTTP_TIMEOUT_TYPE.IdleConnection, value);
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
                return GetTimeSpanTimeout(HttpApiTypes.HTTP_TIMEOUT_TYPE.HeaderWait);
            }
            set
            {
                SetTimeSpanTimeout(HttpApiTypes.HTTP_TIMEOUT_TYPE.HeaderWait, value);
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

                SetUrlGroupTimeouts(_timeouts, (uint)value);
                _minSendBytesPerSecond = (uint)value;
            }
        }

        #endregion Properties

        #region Helpers

        private TimeSpan GetTimeSpanTimeout(HttpApiTypes.HTTP_TIMEOUT_TYPE type)
        {
            // Since we maintain local state, GET is local.
            return new TimeSpan(0, 0, (int)_timeouts[(int)type]);
        }

        private void SetTimeSpanTimeout(HttpApiTypes.HTTP_TIMEOUT_TYPE type, TimeSpan value)
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
            SetUrlGroupTimeouts(newTimeouts, _minSendBytesPerSecond);
            _timeouts[(int)type] = (int)timeoutValue;
        }

        internal void SetUrlGroupTimeouts(UrlGroup urlGroup)
        {
            _urlGroup = urlGroup;
            SetUrlGroupTimeouts(_timeouts, _minSendBytesPerSecond);
        }

        private unsafe void SetUrlGroupTimeouts(int[] timeouts, uint minSendBytesPerSecond)
        {
            if (_urlGroup == null)
            {
                // Not started yet
                return;
            }

            var timeoutinfo = new HttpApiTypes.HTTP_TIMEOUT_LIMIT_INFO();

            timeoutinfo.Flags = HttpApiTypes.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
            timeoutinfo.DrainEntityBody =
                (ushort)timeouts[(int)HttpApiTypes.HTTP_TIMEOUT_TYPE.DrainEntityBody];
            timeoutinfo.EntityBody =
                (ushort)timeouts[(int)HttpApiTypes.HTTP_TIMEOUT_TYPE.EntityBody];
            timeoutinfo.RequestQueue =
                (ushort)timeouts[(int)HttpApiTypes.HTTP_TIMEOUT_TYPE.RequestQueue];
            timeoutinfo.IdleConnection =
                (ushort)timeouts[(int)HttpApiTypes.HTTP_TIMEOUT_TYPE.IdleConnection];
            timeoutinfo.HeaderWait =
                (ushort)timeouts[(int)HttpApiTypes.HTTP_TIMEOUT_TYPE.HeaderWait];
            timeoutinfo.MinSendRate = minSendBytesPerSecond;

            var infoptr = new IntPtr(&timeoutinfo);

            _urlGroup.SetProperty(
                HttpApiTypes.HTTP_SERVER_PROPERTY.HttpServerTimeoutsProperty,
                infoptr, (uint)TimeoutLimitSize);
        }

        #endregion Helpers
    }
}
