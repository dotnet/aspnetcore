// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// These sources are copied from https://github.com/dotnet/roslyn/blob/7d7bf0cc73e335390d73c9de6d7afd1e49605c9d/src/Compilers/CSharp/Portable/Symbols/Synthesized/GeneratedNameConstants.cs
// and exist to address the issues with extracting original method names for
// generated local functions. See https://github.com/dotnet/roslyn/issues/55651
// for more info.
namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class GeneratedNameConstants
    {
        internal const char DotReplacementInTypeNames = '-';
        internal const string SynthesizedLocalNamePrefix = "CS$";
        internal const string SuffixSeparator = "__";
        internal const char LocalFunctionNameTerminator = '|';
    }
}