// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Builder;

namespace Microsoft.AspNet.StaticFiles
{
    /// <summary>
    /// Extension methods for the SendFileMiddleware
    /// </summary>
    public static class SendFileExtensions
    {
        /// <summary>
        /// Provide a SendFile fallback if another component does not.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IBuilder UseSendFileFallback(this IBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            /* TODO builder.GetItem(typeof(ISendFile))

            // Check for advertised support
            if (IsSendFileSupported(builder.Properties))
            {
                return builder;
            }

            // Otherwise, insert a fallback SendFile middleware and advertise support
            SetSendFileCapability(builder.Properties);
             */
            return builder.Use(next => new SendFileMiddleware(next).Invoke);
        }

        private static bool IsSendFileSupported(IDictionary<string, object> properties)
        {
            object obj;
            if (properties.TryGetValue(Constants.ServerCapabilitiesKey, out obj))
            {
                var capabilities = (IDictionary<string, object>)obj;
                if (capabilities.TryGetValue(Constants.SendFileVersionKey, out obj)
                    && Constants.SendFileVersion.Equals((string)obj, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static void SetSendFileCapability(IDictionary<string, object> properties)
        {
            object obj;
            if (properties.TryGetValue(Constants.ServerCapabilitiesKey, out obj))
            {
                var capabilities = (IDictionary<string, object>)obj;
                capabilities[Constants.SendFileVersionKey] = Constants.SendFileVersion;
            }
        }
    }
}