// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// Represents the ID being acknowledged so we can stop buffering older messages.
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
    public long SequenceId { get; set; }
}

/// <summary>
/// Represents the restart of the sequence of messages being sent. <see cref="SequenceId"/> is the starting ID of messages being sent, which might be duplicate messages.
/// </summary>
public sealed class SequenceMessage : HubMessage
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sequenceId"></param>
    public SequenceMessage(long sequenceId)
    {
        SequenceId = sequenceId;
    }

    /// <summary>
    /// 
    /// </summary>
    public long SequenceId { get; set; }
}
