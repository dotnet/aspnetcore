// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor;

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
