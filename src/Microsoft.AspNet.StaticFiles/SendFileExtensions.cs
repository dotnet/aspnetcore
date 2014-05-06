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