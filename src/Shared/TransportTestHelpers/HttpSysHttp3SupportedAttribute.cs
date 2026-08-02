// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Quic;
using Microsoft.Win32;

namespace Microsoft.AspNetCore.InternalTesting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class HttpSysHttp3SupportedAttribute : Attribute, ITestCondition
{
    // We have the same OS and TLS version requirements as MsQuic so check that first.
#pragma warning disable CA2252 // This API requires opting into preview features
    public bool IsMet => QuicListener.IsSupported && IsRegKeySet;
#pragma warning restore CA2252 // This API requires opting into preview features

    public string SkipReason => "HTTP/3 is not supported or enabled on the current test machine";

    private static bool IsRegKeySet
    {
        get
        {
            try
            {
                // Http.Sys requires setting this reg key and rebooting to enable the HTTP/3 preview feature.
                // reg add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\HTTP\Parameters" /v EnableHttp3 /t REG_DWORD /d 1 /f
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\services\HTTP\Parameters");
                var value = key.GetValue("EnableHttp3");
                var enabled = value as int? == 1;
                return enabled;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
