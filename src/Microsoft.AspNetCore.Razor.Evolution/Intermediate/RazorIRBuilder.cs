// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public abstract class RazorIRBuilder
    {
        public static RazorIRBuilder Document()
        {
            var builder = new DefaultRazorIRBuilder();
            builder.Push(new RazorIRDocument());
            return builder;
        }

        public abstract RazorIRNode Current { get; }

        public abstract void Add(RazorIRNode node);

        public abstract void Push(RazorIRNode node);

        public abstract RazorIRNode Pop();
    }
}
