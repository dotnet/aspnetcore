// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorCodeDocument
{
    public static RazorCodeDocument Create(RazorSourceDocument source)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return Create(source, imports: null);
    }

    public static RazorCodeDocument Create(
        RazorSourceDocument source,
        IEnumerable<RazorSourceDocument> imports)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        return new DefaultRazorCodeDocument(source, imports);
    }

    public static RazorCodeDocument Create(
        RazorSourceDocument source,
        IEnumerable<RazorSourceDocument> imports,
        RazorParserOptions parserOptions,
        RazorCodeGenerationOptions codeGenerationOptions)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var codeDocument = new DefaultRazorCodeDocument(source, imports);
        codeDocument.SetParserOptions(parserOptions);
        codeDocument.SetCodeGenerationOptions(codeGenerationOptions);
        return codeDocument;
    }

    public abstract IReadOnlyList<RazorSourceDocument> Imports { get; }

    public abstract ItemCollection Items { get; }

    public abstract RazorSourceDocument Source { get; }
}
