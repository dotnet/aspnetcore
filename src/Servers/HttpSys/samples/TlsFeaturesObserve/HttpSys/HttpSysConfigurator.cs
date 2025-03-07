// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace TlsFeaturesObserve.HttpSys;

internal static class HttpSysConfigurator
{
    const uint HTTP_INITIALIZE_CONFIG = 0x00000002;
    const uint ERROR_ALREADY_EXISTS = 183;

    static readonly HTTPAPI_VERSION HttpApiVersion = new HTTPAPI_VERSION(1, 0);

    internal static void ConfigureCacheTlsClientHello()
    {
        IPEndPoint ipPort = new IPEndPoint(new IPAddress([0, 0, 0, 0]), 6000);
        string certThumbprint = "" /* your cert thumbprint here */;
        Guid appId = Guid.NewGuid();
        string sslCertStoreName = "My";

        CallHttpApi(() => SetConfiguration(ipPort, certThumbprint, appId, sslCertStoreName));
    }

    static void SetConfiguration(IPEndPoint ipPort, string certThumbprint, Guid appId, string sslCertStoreName)
    {
        GCHandle sockAddrHandle = CreateSockaddrStructure(ipPort);
        var pIpPort = sockAddrHandle.AddrOfPinnedObject();
        var httpServiceConfigSslKey = new HTTP_SERVICE_CONFIG_SSL_KEY(pIpPort);

        byte[] hash = GetHash(certThumbprint);
        var handleHash = GCHandle.Alloc(hash, GCHandleType.Pinned);
        var configSslParam = new HTTP_SERVICE_CONFIG_SSL_PARAM
        {
            AppId = appId,
            DefaultFlags = 0x00008000 /* HTTP_SERVICE_CONFIG_SSL_FLAG_ENABLE_CACHE_CLIENT_HELLO */,
            DefaultRevocationFreshnessTime = 0,
            DefaultRevocationUrlRetrievalTimeout = 15,
            pSslCertStoreName = sslCertStoreName,
            pSslHash = handleHash.AddrOfPinnedObject(),
            SslHashLength = hash.Length,
            pDefaultSslCtlIdentifier = null,
            pDefaultSslCtlStoreName = sslCertStoreName
        };

        var configSslSet = new HTTP_SERVICE_CONFIG_SSL_SET
        {
            ParamDesc = configSslParam,
            KeyDesc = httpServiceConfigSslKey
        };

        var pInputConfigInfo = Marshal.AllocCoTaskMem(
            Marshal.SizeOf(typeof(HTTP_SERVICE_CONFIG_SSL_SET)));
        Marshal.StructureToPtr(configSslSet, pInputConfigInfo, false);

        uint status = HttpSetServiceConfiguration(nint.Zero,
            HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo,
            pInputConfigInfo,
            Marshal.SizeOf(configSslSet),
            nint.Zero);

        if (status == ERROR_ALREADY_EXISTS || status == 0) // already present or success
        {
            Console.WriteLine("HttpServiceConfiguration is correct");
        }
        else
        {
            Console.WriteLine("Failed to HttpSetServiceConfiguration: " + status);
        }
    }

    static byte[] GetHash(string thumbprint)
    {
        int length = thumbprint.Length;
        byte[] bytes = new byte[length / 2];
        for (int i = 0; i < length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(thumbprint.Substring(i, 2), 16);
        }

        return bytes;
    }

    static GCHandle CreateSockaddrStructure(IPEndPoint ipEndPoint)
    {
        SocketAddress socketAddress = ipEndPoint.Serialize();

        // use an array of bytes instead of the sockaddr structure 
        byte[] sockAddrStructureBytes = new byte[socketAddress.Size];
        GCHandle sockAddrHandle = GCHandle.Alloc(sockAddrStructureBytes, GCHandleType.Pinned);
        for (int i = 0; i < socketAddress.Size; ++i)
        {
            sockAddrStructureBytes[i] = socketAddress[i];
        }
        return sockAddrHandle;
    }

    static void CallHttpApi(Action body)
    {
        const uint flags = HTTP_INITIALIZE_CONFIG;
        uint retVal = HttpInitialize(HttpApiVersion, flags, IntPtr.Zero);
        body();
    }

// disabled warning since it is just a sample
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    [DllImport("httpapi.dll", SetLastError = true)]
    private static extern uint HttpInitialize(
            HTTPAPI_VERSION version,
            uint flags,
            IntPtr pReserved);

    [DllImport("httpapi.dll", SetLastError = true)]
    public static extern uint HttpSetServiceConfiguration(
        nint serviceIntPtr,
        HTTP_SERVICE_CONFIG_ID configId,
        nint pConfigInformation,
        int configInformationLength,
        nint pOverlapped);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
}
