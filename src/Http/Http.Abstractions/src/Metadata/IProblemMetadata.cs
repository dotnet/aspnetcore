// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// 
/// </summary>
public interface IProblemMetadata
{
    /// <summary>
    /// 
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// 
    /// </summary>
    public ProblemTypes ProblemType { get; }
}
