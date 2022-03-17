// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Represents the HTTP request body as a <see cref="PipeReader"/>.
/// </summary>
public interface IRequestBodyPipeFeature
{
    /// <summary>
    /// Gets a <see cref="PipeReader"/> representing the request body, if any.
    /// </summary>
    PipeReader Reader { get; }
}
