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
}
