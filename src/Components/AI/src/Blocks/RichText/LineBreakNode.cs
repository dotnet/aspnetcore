// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a forced visual line break.
/// </summary>
/// <remarks>
/// This node does not encode a parser-specific soft or hard line-ending distinction. A
/// mapper from Markdig or any other parser decides whether a source line ending becomes
/// whitespace, text, or a <see cref="LineBreakNode"/>.
/// </remarks>
public class LineBreakNode : RichTextNode;
