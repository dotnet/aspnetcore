// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

internal abstract class RazorHtmlDocument
{
    public abstract string GeneratedHtml { get; }

    public abstract RazorCodeGenerationOptions Options { get; }

    public static RazorHtmlDocument Create(string generatedHtml, RazorCodeGenerationOptions options)
    {
        if (generatedHtml == null)
        {
            throw new ArgumentNullException(nameof(generatedHtml));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return new DefaultRazorHtmlDocument(generatedHtml, options);
    }
}
