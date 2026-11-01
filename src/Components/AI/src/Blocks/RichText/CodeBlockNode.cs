// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a block of code.
/// </summary>
public class CodeBlockNode : RichTextNode
{
    /// <summary>
    /// Initializes a new instance of <see cref="CodeBlockNode"/>.
    /// </summary>
    public CodeBlockNode()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CodeBlockNode"/>.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <param name="language">The optional language identifier.</param>
    public CodeBlockNode(string code, string? language = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        Code = code;
        Language = language;
    }

    /// <summary>
    /// Gets or sets the language identifier.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the code.
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
