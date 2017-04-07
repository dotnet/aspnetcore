// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public static class RazorIRBuilderExtensions
    {
        public static void AddAfter<TNode>(this RazorIRBuilder builder, RazorIRNode node)
            where TNode : RazorIRNode
        {
            var children = builder.Current.Children;
            var i = children.Count - 1;
            for (; i >= 0; i--)
            {
                var child = children[i];
                if (child is TNode || child.GetType() == node.GetType())
                {
                    break;
                }
            }

            builder.Insert(i + 1, node);
        }
    }
}
