// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class FirstDirectiveCSharpLanguageCharacteristics : CSharpLanguageCharacteristics
{
    private static readonly FirstDirectiveCSharpLanguageCharacteristics _instance = new FirstDirectiveCSharpLanguageCharacteristics();

    private FirstDirectiveCSharpLanguageCharacteristics()
    {
    }

    public static new FirstDirectiveCSharpLanguageCharacteristics Instance => _instance;

    public override CSharpTokenizer CreateTokenizer(ITextDocument source) => new DirectiveCSharpTokenizer(source);
}
