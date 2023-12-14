// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;

internal abstract partial class AbstractVirtualCharService
{
    /// <summary>
    /// Abstraction to allow generic algorithms to run over a string or <see cref="SourceText"/> without any
    /// overhead.
    /// </summary>
    private interface ITextInfo<T>
    {
        char Get(T text, int index);
        int Length(T text);
    }

    private struct SourceTextTextInfo : ITextInfo<SourceText>
    {
        public char Get(SourceText text, int index) => text[index];
        public int Length(SourceText text) => text.Length;
    }

    private struct StringTextInfo : ITextInfo<string>
    {
        public char Get(string text, int index) => text[index];
        public int Length(string text) => text.Length;
    }
}
