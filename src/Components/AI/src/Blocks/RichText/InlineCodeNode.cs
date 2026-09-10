// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents inline code.
/// </summary>
public class InlineCodeNode : RichTextNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="InlineCodeNode"/>.
    /// </summary>
    public InlineCodeNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InlineCodeNode"/>.
    /// </summary>
    /// <param name="code">The code.</param>
    public InlineCodeNode(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        Code = code;
    }

    /// <summary>
    /// Gets or sets the code.
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
