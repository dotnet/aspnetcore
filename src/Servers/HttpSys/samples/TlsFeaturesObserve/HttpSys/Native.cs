// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TlsFeaturesObserve.HttpSys;

// Http.Sys types from https://learn.microsoft.com/windows/win32/api/http/

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct HTTPAPI_VERSION
{
    public ushort HttpApiMajorVersion;
    public ushort HttpApiMinorVersion;

    public HTTPAPI_VERSION(ushort majorVersion, ushort minorVersion)
    {
        HttpApiMajorVersion = majorVersion;
        HttpApiMinorVersion = minorVersion;
    }
}

public enum HTTP_SERVICE_CONFIG_ID
{
    HttpServiceConfigIPListenList = 0,
    HttpServiceConfigSSLCertInfo,
    HttpServiceConfigUrlAclInfo,
    HttpServiceConfigMax
}

[StructLayout(LayoutKind.Sequential)]
public struct HTTP_SERVICE_CONFIG_SSL_SET
{
    public HTTP_SERVICE_CONFIG_SSL_KEY KeyDesc;
    public HTTP_SERVICE_CONFIG_SSL_PARAM ParamDesc;
}

[StructLayout(LayoutKind.Sequential)]
public struct HTTP_SERVICE_CONFIG_SSL_KEY
{
    public IntPtr pIpPort;

    public HTTP_SERVICE_CONFIG_SSL_KEY(IntPtr pIpPort)
    {
        this.pIpPort = pIpPort;
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct HTTP_SERVICE_CONFIG_SSL_PARAM
{
    public int SslHashLength;
    public IntPtr pSslHash;
    public Guid AppId;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string pSslCertStoreName;
    public CertCheckModes DefaultCertCheckMode;
    public int DefaultRevocationFreshnessTime;
    public int DefaultRevocationUrlRetrievalTimeout;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string pDefaultSslCtlIdentifier;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string pDefaultSslCtlStoreName;
    public uint DefaultFlags; // HTTP_SERVICE_CONFIG_SSL_FLAG
}

[Flags]
public enum CertCheckModes : uint
{
    /// <summary>
    /// Enables the client certificate revocation check.
    /// </summary>
    None = 0,

    /// <summary>
    /// Client certificate is not to be verified for revocation. 
    /// </summary>
    DoNotVerifyCertificateRevocation = 1,

    /// <summary>
    /// Only cached certificate is to be used the revocation check. 
    /// </summary>
    VerifyRevocationWithCachedCertificateOnly = 2,

    /// <summary>
    /// The RevocationFreshnessTime setting is enabled.
    /// </summary>
    EnableRevocationFreshnessTime = 4,

    /// <summary>
    /// No usage check is to be performed.
    /// </summary>
    NoUsageCheck = 0x10000
}
