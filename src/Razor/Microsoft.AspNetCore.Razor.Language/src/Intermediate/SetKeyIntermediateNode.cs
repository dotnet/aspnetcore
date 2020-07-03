// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
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
}
