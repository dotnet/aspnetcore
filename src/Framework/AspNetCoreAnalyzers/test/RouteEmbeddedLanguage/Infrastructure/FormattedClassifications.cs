// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification;

public static partial class FormattedClassifications
{
    private static FormattedClassification New(string text, string typeName)
        => new FormattedClassification(text, typeName);

    [DebuggerStepThrough]
    public static FormattedClassification Struct(string text)
        => New(text, ClassificationTypeNames.StructName);

    [DebuggerStepThrough]
    public static FormattedClassification Enum(string text)
        => New(text, ClassificationTypeNames.EnumName);

    [DebuggerStepThrough]
    public static FormattedClassification Interface(string text)
        => New(text, ClassificationTypeNames.InterfaceName);

    [DebuggerStepThrough]
    public static FormattedClassification Class(string text)
        => New(text, ClassificationTypeNames.ClassName);

    [DebuggerStepThrough]
    public static FormattedClassification Record(string text)
        => New(text, ClassificationTypeNames.RecordClassName);

    [DebuggerStepThrough]
    public static FormattedClassification RecordStruct(string text)
        => New(text, ClassificationTypeNames.RecordStructName);

    [DebuggerStepThrough]
    public static FormattedClassification Delegate(string text)
        => New(text, ClassificationTypeNames.DelegateName);

    [DebuggerStepThrough]
    public static FormattedClassification TypeParameter(string text)
        => New(text, ClassificationTypeNames.TypeParameterName);

    [DebuggerStepThrough]
    public static FormattedClassification Namespace(string text)
        => New(text, ClassificationTypeNames.NamespaceName);

    [DebuggerStepThrough]
    public static FormattedClassification Label(string text)
        => New(text, ClassificationTypeNames.LabelName);

    [DebuggerStepThrough]
    public static FormattedClassification Field(string text)
        => New(text, ClassificationTypeNames.FieldName);

    [DebuggerStepThrough]
    public static FormattedClassification EnumMember(string text)
        => New(text, ClassificationTypeNames.EnumMemberName);

    [DebuggerStepThrough]
    public static FormattedClassification Constant(string text)
        => New(text, ClassificationTypeNames.ConstantName);

    [DebuggerStepThrough]
    public static FormattedClassification Local(string text)
        => New(text, ClassificationTypeNames.LocalName);

    [DebuggerStepThrough]
    public static FormattedClassification Parameter(string text)
        => New(text, ClassificationTypeNames.ParameterName);

    [DebuggerStepThrough]
    public static FormattedClassification Method(string text)
        => New(text, ClassificationTypeNames.MethodName);

    [DebuggerStepThrough]
    public static FormattedClassification ExtensionMethod(string text)
        => New(text, ClassificationTypeNames.ExtensionMethodName);

    [DebuggerStepThrough]
    public static FormattedClassification Property(string text)
        => New(text, ClassificationTypeNames.PropertyName);

    [DebuggerStepThrough]
    public static FormattedClassification Event(string text)
        => New(text, ClassificationTypeNames.EventName);

    [DebuggerStepThrough]
    public static FormattedClassification Static(string text)
        => New(text, ClassificationTypeNames.StaticSymbol);

    [DebuggerStepThrough]
    public static FormattedClassification String(string text)
        => New(text, ClassificationTypeNames.StringLiteral);

    [DebuggerStepThrough]
    public static FormattedClassification Verbatim(string text)
        => New(text, ClassificationTypeNames.VerbatimStringLiteral);

    [DebuggerStepThrough]
    public static FormattedClassification Escape(string text)
        => New(text, ClassificationTypeNames.StringEscapeCharacter);

    [DebuggerStepThrough]
    public static FormattedClassification Keyword(string text)
        => New(text, ClassificationTypeNames.Keyword);

    [DebuggerStepThrough]
    public static FormattedClassification PunctuationText(string text)
        => New(text, ClassificationTypeNames.Punctuation);

    [DebuggerStepThrough]
    public static FormattedClassification ControlKeyword(string text)
        => New(text, ClassificationTypeNames.ControlKeyword);

    [DebuggerStepThrough]
    public static FormattedClassification WhiteSpace(string text)
        => New(text, ClassificationTypeNames.WhiteSpace);

    [DebuggerStepThrough]
    public static FormattedClassification Text(string text)
        => New(text, ClassificationTypeNames.Text);

    [DebuggerStepThrough]
    public static FormattedClassification NumericLiteral(string text)
        => New(text, ClassificationTypeNames.NumericLiteral);

    [DebuggerStepThrough]
    public static FormattedClassification PPKeyword(string text)
        => New(text, ClassificationTypeNames.PreprocessorKeyword);

    [DebuggerStepThrough]
    public static FormattedClassification PPText(string text)
        => New(text, ClassificationTypeNames.PreprocessorText);

    [DebuggerStepThrough]
    public static FormattedClassification Identifier(string text)
        => New(text, ClassificationTypeNames.Identifier);

    [DebuggerStepThrough]
    public static FormattedClassification Inactive(string text)
        => New(text, ClassificationTypeNames.ExcludedCode);

    [DebuggerStepThrough]
    public static FormattedClassification Comment(string text)
        => New(text, ClassificationTypeNames.Comment);

    [DebuggerStepThrough]
    public static FormattedClassification Number(string text)
        => New(text, ClassificationTypeNames.NumericLiteral);

    public static FormattedClassification LineContinuation { get; }
        = New("_", ClassificationTypeNames.Punctuation);

    [DebuggerStepThrough]
    public static FormattedClassification Module(string text)
        => New(text, ClassificationTypeNames.ModuleName);

    [DebuggerStepThrough]
    public static FormattedClassification VBXmlName(string text)
        => New(text, ClassificationTypeNames.XmlLiteralName);

    [DebuggerStepThrough]
    public static FormattedClassification VBXmlText(string text)
        => New(text, ClassificationTypeNames.XmlLiteralText);

    [DebuggerStepThrough]
    public static FormattedClassification VBXmlProcessingInstruction(string text)
        => New(text, ClassificationTypeNames.XmlLiteralProcessingInstruction);

    [DebuggerStepThrough]
    public static FormattedClassification VBXmlEmbeddedExpression(string text)
        => New(text, ClassificationTypeNames.XmlLiteralEmbeddedExpression);

    [DebuggerStepThrough]
    public static FormattedClassification VBXmlDelimiter(string text)
        => New(text, ClassificationTypeNames.XmlLiteralDelimiter);

    [DebuggerStepThrough]
    public static FormattedClassification VBXmlComment(string text)
        => New(text, ClassificationTypeNames.XmlLiteralComment);

    [DebuggerStepThrough]
    public static FormattedClassification VBXmlCDataSection(string text)
        => New(text, ClassificationTypeNames.XmlLiteralCDataSection);

    [DebuggerStepThrough]
    public static FormattedClassification VBXmlAttributeValue(string text)
        => New(text, ClassificationTypeNames.XmlLiteralAttributeValue);

    [DebuggerStepThrough]
    public static FormattedClassification VBXmlAttributeQuotes(string text)
        => New(text, ClassificationTypeNames.XmlLiteralAttributeQuotes);

    [DebuggerStepThrough]
    public static FormattedClassification VBXmlAttributeName(string text)
        => New(text, ClassificationTypeNames.XmlLiteralAttributeName);

    [DebuggerStepThrough]
    public static FormattedClassification VBXmlEntityReference(string text)
        => New(text, ClassificationTypeNames.XmlLiteralEntityReference);
}
