// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

public class LinkReferenceNode : RichTextNode
{
    public string Label { get; set; } = string.Empty;

    public ReferenceKind ReferenceKind { get; set; }
}
