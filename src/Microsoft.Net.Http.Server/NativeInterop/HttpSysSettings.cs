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
// <copyright file="HttpSysSettings.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
#if !DOTNET5_4
using Microsoft.Win32;
#endif

namespace Microsoft.Net.Http.Server
{
    internal static class HttpSysSettings
    {
#if !DOTNET5_4
        private const string HttpSysParametersKey = @"System\CurrentControlSet\Services\HTTP\Parameters";
#endif
        private const bool EnableNonUtf8Default = true;
        private const bool FavorUtf8Default = true;
        private const string EnableNonUtf8Name = "EnableNonUtf8";
        private const string FavorUtf8Name = "FavorUtf8";

        private static volatile bool enableNonUtf8 = EnableNonUtf8Default;
        private static volatile bool favorUtf8 = FavorUtf8Default;

        static HttpSysSettings()
        {
            ReadHttpSysRegistrySettings();
        }

        internal static bool EnableNonUtf8
        {
            get { return enableNonUtf8; }
        }

        internal static bool FavorUtf8
        {
            get { return favorUtf8; }
        }

        private static void ReadHttpSysRegistrySettings()
#if DOTNET5_4
        {
        }
#else
        {
            try
            {
                RegistryKey httpSysParameters = Registry.LocalMachine.OpenSubKey(HttpSysParametersKey);

                if (httpSysParameters == null)
                {
                    LogWarning("ReadHttpSysRegistrySettings", "The Http.Sys registry key is null.",
                        HttpSysParametersKey);
                }
                else
                {
                    using (httpSysParameters)
                    {
                        enableNonUtf8 = ReadRegistryValue(httpSysParameters, EnableNonUtf8Name, EnableNonUtf8Default);
                        favorUtf8 = ReadRegistryValue(httpSysParameters, FavorUtf8Name, FavorUtf8Default);
                    }
                }
            }
            catch (SecurityException e)
            {
                LogRegistryException("ReadHttpSysRegistrySettings", e);
            }
            catch (ObjectDisposedException e)
            {
                LogRegistryException("ReadHttpSysRegistrySettings", e);
            }
        }

        private static bool ReadRegistryValue(RegistryKey key, string valueName, bool defaultValue)
        {
            Debug.Assert(key != null, "'key' must not be null");

            try
            {
                if (key.GetValue(valueName) != null && key.GetValueKind(valueName) == RegistryValueKind.DWord)
                {
                    // At this point we know the Registry value exists and it must be valid (any DWORD value
                    // can be converted to a bool).
                    return Convert.ToBoolean(key.GetValue(valueName), CultureInfo.InvariantCulture);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                LogRegistryException("ReadRegistryValue", e);
            }
            catch (IOException e)
            {
                LogRegistryException("ReadRegistryValue", e);
            }
            catch (SecurityException e)
            {
                LogRegistryException("ReadRegistryValue", e);
            }
            catch (ObjectDisposedException e)
            {
                LogRegistryException("ReadRegistryValue", e);
            }

            return defaultValue;
        }

        private static void LogRegistryException(string methodName, Exception e)
        {
            LogWarning(methodName, "Unable to access the Http.Sys registry value.", HttpSysParametersKey, e);
        }

        private static void LogWarning(string methodName, string message, params object[] args)
        {
            // TODO: log
            // Logging.PrintWarning(Logging.HttpListener, typeof(HttpSysSettings), methodName, SR.GetString(message, args));
        }
#endif
    }
}
