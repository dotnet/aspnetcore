// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{
    /// <summary>
    /// These utilities are designed to test openidconnect related flows
    /// </summary>
    public class TestUtilities
    {
        public const string DefaultHost = @"http://localhost";

        public static bool AreEqual<T>(object obj1, object obj2, Func<object, object, bool> comparer = null) where T : class
        {
            if (obj1 == null && obj2 == null)
            {
                return true;
            }

            if (obj1 == null || obj2 == null)
            {
                return false;
            }

            if (obj1.GetType() != obj2.GetType())
            {
                return false;
            }

            if (obj1.GetType() != typeof(T))
            {
                return false;
            }

            if (comparer != null)
            {
                return comparer(obj1, obj2);
            }

            if (typeof(T) == typeof(LogEntry))
            {
                return AreEqual(obj1 as LogEntry, obj2 as LogEntry);
            }
            else if (typeof(T) == typeof(Exception))
            {
                return AreEqual(obj1 as Exception, obj2 as Exception);
            }

            throw new ArithmeticException("Unknown type, no comparer. Type: " + typeof(T).ToString());

        }

        /// <summary>
        /// Never call this method directly, call AreObjectsEqual, as it deals with nulls and types"/>
        /// </summary>
        /// <param name="logEntry1"></param>
        /// <param name="logEntry2"></param>
        /// <returns></returns>
        private static bool AreEqual(LogEntry logEntry1, LogEntry logEntry2)
        {
            if (logEntry1.EventId != logEntry2.EventId)
            {
                return false;
            }

            if (logEntry1.State == null && logEntry2.State == null)
            {
                return true;
            }

            if (logEntry1.State == null)
            {
                return false;
            }

            if (logEntry2.State == null)
            {
                return false;
            }

            string logValue1 = logEntry1.Formatter == null ? logEntry1.State.ToString() : logEntry1.Formatter(logEntry1.State, logEntry1.Exception);
            string logValue2 = logEntry2.Formatter == null ? logEntry2.State.ToString() : logEntry2.Formatter(logEntry2.State, logEntry2.Exception);

            return (logValue1.StartsWith(logValue2) || (logValue2.StartsWith(logValue1)));
        }

        /// <summary>
        /// Never call this method directly, call AreObjectsEqual, as it deals with nulls and types"/>
        /// </summary>
        /// <param name="exception1"></param>
        /// <param name="exception2"></param>
        /// <returns></returns>
        private static bool AreEqual(Exception exception1, Exception exception2)
        {
            if (!string.Equals(exception1.Message, exception2.Message))
            {
                return false;
            }

            return AreEqual<Exception>(exception1.InnerException, exception2.InnerException);
        }

        static public IConfigurationManager<OpenIdConnectConfiguration> DefaultOpenIdConnectConfigurationManager
        {
            get
            {
                return new StaticConfigurationManager<OpenIdConnectConfiguration>(DefaultOpenIdConnectConfiguration);
            }
        }

        static public OpenIdConnectConfiguration DefaultOpenIdConnectConfiguration
        {
            get
            {
                return new OpenIdConnectConfiguration()
                {
                    AuthorizationEndpoint = @"https://login.windows.net/common/oauth2/authorize",
                    EndSessionEndpoint = @"https://login.windows.net/common/oauth2/endsessionendpoint",
                    TokenEndpoint = @"https://login.windows.net/common/oauth2/token",
                };
            }
        }
    }
}
