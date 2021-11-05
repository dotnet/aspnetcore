// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class SetKeyIntermediateNode : IntermediateNode
{
    public SetKeyIntermediateNode(IntermediateToken keyValueToken)
    {
        KeyValueToken = keyValueToken ?? throw new ArgumentNullException(nameof(keyValueToken));
        Source = KeyValueToken.Source;
    }

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public IntermediateToken KeyValueToken { get; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitSetKey(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        if (formatter == null)
        {
            throw new ArgumentNullException(nameof(formatter));
        }

        formatter.WriteContent(KeyValueToken.Content);

        formatter.WriteProperty(nameof(KeyValueToken), KeyValueToken.Content);
    }
}
