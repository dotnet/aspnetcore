// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorHtmlDocument : RazorHtmlDocument
{
    private readonly string _generatedHtml;
    private readonly RazorCodeGenerationOptions _options;

    public DefaultRazorHtmlDocument(
        string generatedHtml,
        RazorCodeGenerationOptions options)
    {
        if (generatedHtml == null)
        {
            throw new ArgumentNullException(nameof(generatedHtml));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _generatedHtml = generatedHtml;
        _options = options;
    }

    public override string GeneratedHtml => _generatedHtml;

    public override RazorCodeGenerationOptions Options => _options;
}
