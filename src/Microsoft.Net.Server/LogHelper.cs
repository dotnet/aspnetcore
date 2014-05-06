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

//------------------------------------------------------------------------------
// <copyright file="HttpListener.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Framework.Logging;

namespace Microsoft.Net.Server
{
    internal static class LogHelper
    {
        internal static ILogger CreateLogger(ILoggerFactory factory, Type type)
        {
            if (factory == null)
            {
                return null;
            }

            return factory.Create(type.FullName);
        }

        internal static void LogInfo(ILogger logger, string data)
        {
            if (logger == null)
            {
                Debug.WriteLine(data);
            }
            else
            {
                logger.WriteInformation(data);
            }
        }

        internal static void LogVerbose(ILogger logger, string data)
        {
            if (logger == null)
            {
                Debug.WriteLine(data);
            }
            else
            {
                logger.WriteVerbose(data);
            }
        }

        internal static void LogException(ILogger logger, string location, Exception exception)
        {
            if (logger == null)
            {
                Debug.WriteLine(exception);
            }
            else
            {
                logger.WriteError(location, exception);
            }
        }

        internal static void LogError(ILogger logger, string location, string message)
        {
            if (logger == null)
            {
                Debug.WriteLine(message);
            }
            else
            {
                logger.WriteError(location + "; " + message);
            }
        }
    }
}
