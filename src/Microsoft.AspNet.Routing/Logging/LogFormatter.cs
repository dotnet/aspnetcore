// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Routing.Logging.Internal
{
    public static class LogFormatter
    {
        /// <summary>
        /// A formatter for use with <see cref="Microsoft.Framework.Logging.ILogger.Write"/>.
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