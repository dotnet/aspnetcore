// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal static class HttpSysSettings
    {
        private const string HttpSysParametersKey = @"System\CurrentControlSet\Services\HTTP\Parameters";
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
    }
}
