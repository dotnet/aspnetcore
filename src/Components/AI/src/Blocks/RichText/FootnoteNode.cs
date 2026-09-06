// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents footnote content supplied by an application's rich-text mapper.
/// </summary>
/// <remarks>
/// Parsers model footnotes differently. For example, Markdig exposes footnote containers,
/// links, and an end-of-document footnote group. A mapper can project those parser-specific
/// objects into the footnote presentation nodes without introducing a parser dependency.
/// </remarks>
public class FootnoteNode : RichTextNode;
