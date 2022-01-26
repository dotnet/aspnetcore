// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// Type returned to <see cref="IHubProtocol"/> implementations to let them know the object being deserialized should be
/// stored as raw serialized bytes in the format of the protocol being used.
/// </summary>
/// <example>
/// In Json that would mean storing the bytes of {"prop":10} as an example.
/// </example>
public class RawResult
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="rawBytes"></param>
    public RawResult(ReadOnlySequence<byte> rawBytes)
    {
        RawSerializedData = rawBytes;
    }

    /// <summary>
    /// 
    /// </summary>
    public ReadOnlySequence<byte> RawSerializedData { get; private set; }
}
