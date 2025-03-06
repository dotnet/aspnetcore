// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Abstractions.TLS;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// Represents the details about the TLS fingerprinting.
/// </summary>
public interface ITlsFingerprintingFeature
{
    /// <summary>
    /// Returns the TLS client hello details, if any.
    /// </summary>
    TLS_CLIENT_HELLO GetTlsClientHello();
}
