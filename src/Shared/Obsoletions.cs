// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Shared;

internal sealed class Obsoletions
{
    internal const string RuntimeSharedUrlFormat = "https://aka.ms/dotnet-warnings/{0}";

    internal const string RuntimeTlsCipherAlgorithmEnumsMessage = "KeyExchangeAlgorithm, KeyExchangeStrength, CipherAlgorithm, CipherStrength, HashAlgorithm and HashStrength properties are obsolete. Use NegotiatedCipherSuite instead.";
    internal const string RuntimeTlsCipherAlgorithmEnumsDiagId = "SYSLIB0058";

    // ASP.NET Core deprecated API URLs (not using {0} placeholder - these are explicit URLs)
    internal const string AspNetCoreDeprecate002Url = "https://aka.ms/aspnet/deprecate/002";
    internal const string AspNetCoreDeprecate003Url = "https://aka.ms/aspnet/deprecate/003";
    internal const string AspNetCoreDeprecate004Url = "https://aka.ms/aspnet/deprecate/004";
    internal const string AspNetCoreDeprecate005Url = "https://aka.ms/aspnet/deprecate/005";
    internal const string AspNetCoreDeprecate006Url = "https://aka.ms/aspnet/deprecate/006";
    internal const string AspNetCoreDeprecate008Url = "https://aka.ms/aspnet/deprecate/008";
    internal const string AspNetCoreDeprecate009Url = "https://aka.ms/aspnet/deprecate/009";
}
