// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public interface IExtensionIntermediateNodeVisitor<TNode> where TNode : ExtensionIntermediateNode
{
    void VisitExtension(TNode node);
}
