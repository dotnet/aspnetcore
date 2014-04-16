// -----------------------------------------------------------------------
// <copyright file="TimeoutManager.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Net.Server
{
    // See the native HTTP_TIMEOUT_LIMIT_INFO structure documentation for additional information.
    // http://msdn.microsoft.com/en-us/library/aa364661.aspx

    /// <summary>
    /// Exposes the Http.Sys timeout configurations.  These may also be configured in the registry.
    /// </summary>
    public sealed class TimeoutManager
    {
#if NET45
        private static readonly int TimeoutLimitSize =
            Marshal.SizeOf(typeof(UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_LIMIT_INFO));
#else
        private static readonly int TimeoutLimitSize =
            Marshal.SizeOf<UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_LIMIT_INFO>();
#endif
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

            LoadConfigurationSettings();
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
                return GetTimeout(UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.EntityBody);
            }
            set
            {
                SetTimespanTimeout(UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.EntityBody, value);
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
                return GetTimeout(UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.DrainEntityBody);
            }
            set
            {
                SetTimespanTimeout(UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.DrainEntityBody, value);
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
                return GetTimeout(UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.RequestQueue);
            }
            set
            {
                SetTimespanTimeout(UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.RequestQueue, value);
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
                return GetTimeout(UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.IdleConnection);
            }
            set
            {
                SetTimespanTimeout(UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.IdleConnection, value);
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
                return GetTimeout(UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.HeaderWait);
            }
            set
            {
                SetTimespanTimeout(UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.HeaderWait, value);
            }
        }

        /// <summary>
        /// The minimum send rate, in bytes-per-second, for the response. The default response send rate is 150 
        /// bytes-per-second.
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

                SetServerTimeout(_timeouts, (uint)value);
                _minSendBytesPerSecond = (uint)value;
            }
        }

        #endregion Properties

        // Initial values come from the config.  The values can then be overridden using this public API.
        private void LoadConfigurationSettings()
        {
            long[] configTimeouts = new long[_timeouts.Length + 1]; // SettingsSectionInternal.Section.HttpListenerTimeouts;
            Debug.Assert(configTimeouts != null);
            Debug.Assert(configTimeouts.Length == (_timeouts.Length + 1));

            bool setNonDefaults = false;
            for (int i = 0; i < _timeouts.Length; i++)
            {
                if (configTimeouts[i] != 0)
                {
                    Debug.Assert(configTimeouts[i] <= ushort.MaxValue, "Timeout out of range: " + configTimeouts[i]);
                    _timeouts[i] = (int)configTimeouts[i];
                    setNonDefaults = true;
                }
            }

            if (configTimeouts[5] != 0)
            {
                Debug.Assert(configTimeouts[5] <= uint.MaxValue, "Timeout out of range: " + configTimeouts[5]);
                _minSendBytesPerSecond = (uint)configTimeouts[5];
                setNonDefaults = true;
            }

            if (setNonDefaults)
            {
                SetServerTimeout(_timeouts, _minSendBytesPerSecond);
            }
        }

        #region Helpers

        private TimeSpan GetTimeout(UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE type)
        {
            // Since we maintain local state, GET is local.
            return new TimeSpan(0, 0, (int)_timeouts[(int)type]);
        }

        private void SetTimespanTimeout(UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE type, TimeSpan value)
        {
            Int64 timeoutValue;

            // All timeouts are defined as USHORT in native layer (except MinSendRate, which is ULONG). Make sure that
            // timeout value is within range.

            timeoutValue = Convert.ToInt64(value.TotalSeconds);

            if (timeoutValue < 0 || timeoutValue > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException("value");
            }

            // Use local state to get values for other timeouts. Call into the native layer and if that 
            // call succeeds, update local state.

            int[] currentTimeouts = _timeouts;
            currentTimeouts[(int)type] = (int)timeoutValue;
            SetServerTimeout(currentTimeouts, _minSendBytesPerSecond);
            _timeouts[(int)type] = (int)timeoutValue;
        }

        private unsafe void SetServerTimeout(int[] timeouts, uint minSendBytesPerSecond)
        {
            UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_LIMIT_INFO timeoutinfo =
                new UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_LIMIT_INFO();

            timeoutinfo.Flags = UnsafeNclNativeMethods.HttpApi.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
            timeoutinfo.DrainEntityBody =
                (ushort)timeouts[(int)UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.DrainEntityBody];
            timeoutinfo.EntityBody =
                (ushort)timeouts[(int)UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.EntityBody];
            timeoutinfo.RequestQueue =
                (ushort)timeouts[(int)UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.RequestQueue];
            timeoutinfo.IdleConnection =
                (ushort)timeouts[(int)UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.IdleConnection];
            timeoutinfo.HeaderWait =
                (ushort)timeouts[(int)UnsafeNclNativeMethods.HttpApi.HTTP_TIMEOUT_TYPE.HeaderWait];
            timeoutinfo.MinSendRate = minSendBytesPerSecond;

            IntPtr infoptr = new IntPtr(&timeoutinfo);

            _server.SetUrlGroupProperty(
                UnsafeNclNativeMethods.HttpApi.HTTP_SERVER_PROPERTY.HttpServerTimeoutsProperty,
                infoptr, (uint)TimeoutLimitSize);
        }

        #endregion Helpers
    }
}
