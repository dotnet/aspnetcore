// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    public static class TagHelperDescriptorProviderContextExtensions
    {
        public static Compilation GetCompilation(this TagHelperDescriptorProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return (Compilation)context.Items[typeof(Compilation)];
        }

        public static void SetCompilation(this TagHelperDescriptorProviderContext context, Compilation compilation)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.Items[typeof(Compilation)] = compilation;
        }
    }
}
