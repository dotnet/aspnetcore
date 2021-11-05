// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class FirstDirectiveHtmlLanguageCharacteristics : HtmlLanguageCharacteristics
{
    private static readonly FirstDirectiveHtmlLanguageCharacteristics _instance = new FirstDirectiveHtmlLanguageCharacteristics();

    private FirstDirectiveHtmlLanguageCharacteristics()
    {
    }

    public static new FirstDirectiveHtmlLanguageCharacteristics Instance => _instance;

    public override HtmlTokenizer CreateTokenizer(ITextDocument source) => new DirectiveHtmlTokenizer(source);
}
