// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

internal static class EmbeddedLanguagesTestConstants
{
    public static readonly string StringSyntaxAttributeCodeCSharp = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class StringSyntaxAttribute : Attribute
    {
        public StringSyntaxAttribute(string syntax)
        {
            Syntax = syntax;
            Arguments = Array.Empty<object?>();
        }

        public StringSyntaxAttribute(string syntax, params object?[] arguments)
        {
            Syntax = syntax;
            Arguments = arguments;
        }

        public string Syntax { get; }
        public object?[] Arguments { get; }

        public const string DateTimeFormat = nameof(DateTimeFormat);
        public const string Json = nameof(Json);
        public const string Regex = nameof(Regex);
    }
}
";
}
