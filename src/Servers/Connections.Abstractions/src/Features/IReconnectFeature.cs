// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections.Abstractions;

/// <summary>
/// 
/// </summary>
public interface IReconnectFeature
{
    /// <summary>
    /// 
    /// </summary>
    public Action<PipeWriter> NotifyOnReconnect { get; set; }

    /// <summary>
    /// 
    /// </summary>
    void DisableReconnect();
}
