// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    internal abstract class IntermediateNodeBuilder
    {
        public static IntermediateNodeBuilder Create(IntermediateNode root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var builder = new DefaultRazorIntermediateNodeBuilder();
            builder.Push(root);
            return builder;
        }

        public abstract IntermediateNode Current { get; }

        public abstract void Add(IntermediateNode node);

        public abstract void Insert(int index, IntermediateNode node);

        public abstract IntermediateNode Build();

        public abstract void Push(IntermediateNode node);

        public abstract IntermediateNode Pop();
    }
}
