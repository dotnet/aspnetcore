// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class FirstDirectiveCSharpLanguageCharacteristics : CSharpLanguageCharacteristics
    {
        private static readonly FirstDirectiveCSharpLanguageCharacteristics _instance = new FirstDirectiveCSharpLanguageCharacteristics();

        private FirstDirectiveCSharpLanguageCharacteristics()
        {
        }

        public new static FirstDirectiveCSharpLanguageCharacteristics Instance => _instance;

        public override CSharpTokenizer CreateTokenizer(ITextDocument source) => new DirectiveCSharpTokenizer(source);
    }
}
