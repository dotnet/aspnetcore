// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// A marker class used to determine if QUIC support was requested,
/// regardless or whether or not it is supported.
/// TODO (acasey): can we make this non-public?
/// </summary>
public sealed class MultiplexedConnectionMarkerService
{
}
