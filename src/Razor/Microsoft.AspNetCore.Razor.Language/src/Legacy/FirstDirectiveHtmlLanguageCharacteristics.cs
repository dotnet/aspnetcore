// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class FirstDirectiveHtmlLanguageCharacteristics : HtmlLanguageCharacteristics
    {
        private static readonly FirstDirectiveHtmlLanguageCharacteristics _instance = new FirstDirectiveHtmlLanguageCharacteristics();

        private FirstDirectiveHtmlLanguageCharacteristics()
        {
        }

        public new static FirstDirectiveHtmlLanguageCharacteristics Instance => _instance;

        public override HtmlTokenizer CreateTokenizer(ITextDocument source) => new DirectiveHtmlTokenizer(source);
    }
}
