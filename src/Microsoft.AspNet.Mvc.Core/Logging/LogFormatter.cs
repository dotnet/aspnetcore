// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Logging
{
    public static class LogFormatter
    {
        /// <summary>
        /// A formatter for use with <see cref="Microsoft.Framework.Logging.ILogger.Write(
        /// Framework.Logging.TraceType, 
        /// int, 
        /// object, 
        /// Exception, Func{object, Exception, string})"/>.
        /// </summary>
        public static string Formatter(object o, Exception e)
        {
            if (o != null && e != null)
            {
                return o + Environment.NewLine + e;
            }

            if (o != null)
            {
                return o.ToString();
            }

            if (e != null)
            {
                return e.ToString();
            }

            return "";
        }
    }
}