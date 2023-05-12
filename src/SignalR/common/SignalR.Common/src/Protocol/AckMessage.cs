// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// 
/// </summary>
public sealed class AckMessage : HubMessage
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sequenceId"></param>
    public AckMessage(long sequenceId)
    {
        SequenceId = sequenceId;
    }

    /// <summary>
    /// 
    /// </summary>
    public long SequenceId { get; }
}
