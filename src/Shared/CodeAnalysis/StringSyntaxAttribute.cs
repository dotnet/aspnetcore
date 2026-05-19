// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Specifies the syntax used in a string.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class StringSyntaxAttribute : Attribute
{
    /// <summary>Initializes the <see cref="StringSyntaxAttribute"/> with the identifier of the syntax used.</summary>
    /// <param name="syntax">The syntax identifier.</param>
    public StringSyntaxAttribute(string syntax)
    {
        Syntax = syntax;
        Arguments = Array.Empty<object?>();
    }

    /// <summary>Initializes the <see cref="StringSyntaxAttribute"/> with the identifier of the syntax used.</summary>
    /// <param name="syntax">The syntax identifier.</param>
    /// <param name="arguments">Optional arguments associated with the specific syntax employed.</param>
    public StringSyntaxAttribute(string syntax, params object?[] arguments)
    {
        Syntax = syntax;
        Arguments = arguments;
    }

    /// <summary>Gets the identifier of the syntax used.</summary>
    public string Syntax { get; }

    /// <summary>Optional arguments associated with the specific syntax employed.</summary>
    public object?[] Arguments { get; }

    /// <summary>The syntax identifier for strings containing composite formats for string formatting.</summary>
    public const string CompositeFormat = nameof(CompositeFormat);

    /// <summary>The syntax identifier for strings containing date format specifiers.</summary>
    public const string DateOnlyFormat = nameof(DateOnlyFormat);

    /// <summary>The syntax identifier for strings containing date and time format specifiers.</summary>
    public const string DateTimeFormat = nameof(DateTimeFormat);

    /// <summary>The syntax identifier for strings containing <see cref="Enum"/> format specifiers.</summary>
    public const string EnumFormat = nameof(EnumFormat);

    /// <summary>The syntax identifier for strings containing <see cref="Guid"/> format specifiers.</summary>
    public const string GuidFormat = nameof(GuidFormat);

    /// <summary>The syntax identifier for strings containing JavaScript Object Notation (JSON).</summary>
    public const string Json = nameof(Json);

    /// <summary>The syntax identifier for strings containing numeric format specifiers.</summary>
    public const string NumericFormat = nameof(NumericFormat);

    /// <summary>The syntax identifier for strings containing regular expressions.</summary>
    public const string Regex = nameof(Regex);

    /// <summary>The syntax identifier for strings containing time format specifiers.</summary>
    public const string TimeOnlyFormat = nameof(TimeOnlyFormat);

    /// <summary>The syntax identifier for strings containing <see cref="TimeSpan"/> format specifiers.</summary>
    public const string TimeSpanFormat = nameof(TimeSpanFormat);

    /// <summary>The syntax identifier for strings containing URIs.</summary>
    public const string Uri = nameof(Uri);

    /// <summary>The syntax identifier for strings containing XML.</summary>
    public const string Xml = nameof(Xml);
}
#endif
