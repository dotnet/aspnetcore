// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// The way a directive can be used. The usage determines how many, and where directives can exist per file.
/// </summary>
public enum DirectiveUsage
{
    /// <summary>
    /// Directive can exist anywhere in the file.
    /// </summary>
    Unrestricted,

    /// <summary>
    /// Directive must exist prior to any HTML or code and have no duplicates. When importing the directive, if it is 
    /// <see cref="DirectiveKind.SingleLine"/>, the last occurrence of the directive is imported.
    /// </summary>
    FileScopedSinglyOccurring,

    /// <summary>
    /// Directive must exist prior to any HTML or code.
    /// </summary>
    FileScopedMultipleOccurring,
}
