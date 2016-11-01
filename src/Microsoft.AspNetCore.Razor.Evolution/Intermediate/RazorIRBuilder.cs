// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public abstract class RazorIRBuilder
    {
        public static RazorIRBuilder Document()
        {
            return Create(new DocumentIRNode());
        }

        public static RazorIRBuilder Create(RazorIRNode root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var builder = new DefaultRazorIRBuilder();
            builder.Push(root);
            return builder;
        }

        public abstract RazorIRNode Current { get; }

        public abstract void Add(RazorIRNode node);

        public abstract RazorIRNode Build();

        public abstract void Push(RazorIRNode node);

        public abstract RazorIRNode Pop();
    }
}
