// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents binary content, such as an image, audio clip, video, or file, in a conversation.
/// </summary>
public class DataContentBlock : ContentBlock
{
    /// <summary>
    /// Gets or sets the binary content represented by this block.
    /// </summary>
    public DataContent Content { get; set; } = default!;
}
