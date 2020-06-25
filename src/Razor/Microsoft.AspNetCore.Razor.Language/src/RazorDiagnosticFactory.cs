// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal static class RazorDiagnosticFactory
    {
        private const string DiagnosticPrefix = "RZ";

        #region General Errors

        // General Errors ID Offset = 0

        internal static readonly RazorDiagnosticDescriptor Directive_BlockDirectiveCannotBeImported =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}0000",
                () => Resources.BlockDirectiveCannotBeImported,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateDirective_BlockDirectiveCannotBeImported(string directive)
        {
            return RazorDiagnostic.Create(Directive_BlockDirectiveCannotBeImported, SourceSpan.Undefined, directive);
        }

        #endregion

        #region Language Errors

        // Language Errors ID Offset = 1000

        internal static readonly RazorDiagnosticDescriptor Parsing_UnterminatedStringLiteral =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1000",
                () => Resources.ParseError_Unterminated_String_Literal,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_UnterminatedStringLiteral(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_UnterminatedStringLiteral, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_BlockCommentNotTerminated =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1001",
                () => Resources.ParseError_BlockComment_Not_Terminated,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_BlockCommentNotTerminated(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_BlockCommentNotTerminated, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_HelperDirectiveNotAvailable =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1002",
                () => Resources.ParseError_HelperDirectiveNotAvailable,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_HelperDirectiveNotAvailable(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_HelperDirectiveNotAvailable, location, SyntaxConstants.CSharp.HelperKeyword);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedWhiteSpaceAtStartOfCodeBlock =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1003",
                () => Resources.ParseError_Unexpected_WhiteSpace_At_Start_Of_CodeBlock,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_UnexpectedWhiteSpaceAtStartOfCodeBlock(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_UnexpectedWhiteSpaceAtStartOfCodeBlock, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedEndOfFileAtStartOfCodeBlock =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1004",
                () => Resources.ParseError_Unexpected_EndOfFile_At_Start_Of_CodeBlock,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_UnexpectedEndOfFileAtStartOfCodeBlock(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_UnexpectedEndOfFileAtStartOfCodeBlock, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedCharacterAtStartOfCodeBlock =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1005",
                () => Resources.ParseError_Unexpected_Character_At_Start_Of_CodeBlock,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_UnexpectedCharacterAtStartOfCodeBlock(SourceSpan location, string content)
        {
            return RazorDiagnostic.Create(Parsing_UnexpectedCharacterAtStartOfCodeBlock, location, content);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_ExpectedEndOfBlockBeforeEOF =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1006",
                () => Resources.ParseError_Expected_EndOfBlock_Before_EOF,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_ExpectedEndOfBlockBeforeEOF(SourceSpan location, string blockName, string closeBlock, string openBlock)
        {
            return RazorDiagnostic.Create(Parsing_ExpectedEndOfBlockBeforeEOF, location, blockName, closeBlock, openBlock);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_ReservedWord =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1007",
                () => Resources.ParseError_ReservedWord,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_ReservedWord(SourceSpan location, string content)
        {
            return RazorDiagnostic.Create(Parsing_ReservedWord, location, content);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_SingleLineControlFlowStatementsNotAllowed =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1008",
                () => Resources.ParseError_SingleLine_ControlFlowStatements_CannotContainMarkup,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_SingleLineControlFlowStatementsCannotContainMarkup(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_SingleLineControlFlowStatementsNotAllowed, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_AtInCodeMustBeFollowedByColonParenOrIdentifierStart =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1009",
                () => Resources.ParseError_AtInCode_Must_Be_Followed_By_Colon_Paren_Or_Identifier_Start,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_AtInCodeMustBeFollowedByColonParenOrIdentifierStart(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_AtInCodeMustBeFollowedByColonParenOrIdentifierStart, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedNestedCodeBlock =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1010",
                () => Resources.ParseError_Unexpected_Nested_CodeBlock,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_UnexpectedNestedCodeBlock(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_UnexpectedNestedCodeBlock, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveTokensMustBeSeparatedByWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1011",
                () => Resources.DirectiveTokensMustBeSeparatedByWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_DirectiveTokensMustBeSeparatedByWhitespace(SourceSpan location, string directiveName)
        {
            return RazorDiagnostic.Create(Parsing_DirectiveTokensMustBeSeparatedByWhitespace, location, directiveName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedEOFAfterDirective =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1012",
                () => Resources.UnexpectedEOFAfterDirective,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_UnexpectedEOFAfterDirective(SourceSpan location, string directiveName, string expectedToken)
        {
            return RazorDiagnostic.Create(Parsing_UnexpectedEOFAfterDirective, location, directiveName, expectedToken);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveExpectsTypeName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1013",
                () => Resources.DirectiveExpectsTypeName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_DirectiveExpectsTypeName(SourceSpan location, string directiveName)
        {
            return RazorDiagnostic.Create(Parsing_DirectiveExpectsTypeName, location, directiveName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveExpectsNamespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1014",
                () => Resources.DirectiveExpectsNamespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_DirectiveExpectsNamespace(SourceSpan location, string directiveName)
        {
            return RazorDiagnostic.Create(Parsing_DirectiveExpectsNamespace, location, directiveName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveExpectsIdentifier =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1015",
                () => Resources.DirectiveExpectsIdentifier,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_DirectiveExpectsIdentifier(SourceSpan location, string directiveName)
        {
            return RazorDiagnostic.Create(Parsing_DirectiveExpectsIdentifier, location, directiveName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveExpectsQuotedStringLiteral =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1016",
                () => Resources.DirectiveExpectsQuotedStringLiteral,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_DirectiveExpectsQuotedStringLiteral(SourceSpan location, string directiveName)
        {
            return RazorDiagnostic.Create(Parsing_DirectiveExpectsQuotedStringLiteral, location, directiveName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedDirectiveLiteral =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1017",
                () => Resources.UnexpectedDirectiveLiteral,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_UnexpectedDirectiveLiteral(SourceSpan location, string directiveName, string expected)
        {
            return RazorDiagnostic.Create(Parsing_UnexpectedDirectiveLiteral, location, directiveName, expected);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveMustHaveValue =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1018",
                () => Resources.ParseError_DirectiveMustHaveValue,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_DirectiveMustHaveValue(SourceSpan location, string directiveName)
        {
            return RazorDiagnostic.Create(Parsing_DirectiveMustHaveValue, location, directiveName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_IncompleteQuotesAroundDirective =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1019",
                () => Resources.ParseError_IncompleteQuotesAroundDirective,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_IncompleteQuotesAroundDirective(SourceSpan location, string directiveName)
        {
            return RazorDiagnostic.Create(Parsing_IncompleteQuotesAroundDirective, location, directiveName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_InvalidTagHelperPrefixValue =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1020",
                () => Resources.InvalidTagHelperPrefixValue,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_InvalidTagHelperPrefixValue(SourceSpan location, string directiveName, char character, string prefix)
        {
            return RazorDiagnostic.Create(Parsing_InvalidTagHelperPrefixValue, location, directiveName, character, prefix);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_MarkupBlockMustStartWithTag =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1021",
                () => Resources.ParseError_MarkupBlock_Must_Start_With_Tag,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_MarkupBlockMustStartWithTag(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_MarkupBlockMustStartWithTag, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_OuterTagMissingName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1022",
                () => Resources.ParseError_OuterTagMissingName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_OuterTagMissingName(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_OuterTagMissingName, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_TextTagCannotContainAttributes =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1023",
                () => Resources.ParseError_TextTagCannotContainAttributes,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_TextTagCannotContainAttributes(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_TextTagCannotContainAttributes, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_UnfinishedTag =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1024",
                () => Resources.ParseError_UnfinishedTag,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_UnfinishedTag(SourceSpan location, string tagName)
        {
            return RazorDiagnostic.Create(Parsing_UnfinishedTag, location, tagName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_MissingEndTag =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1025",
                () => Resources.ParseError_MissingEndTag,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_MissingEndTag(SourceSpan location, string tagName)
        {
            return RazorDiagnostic.Create(Parsing_MissingEndTag, location, tagName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_UnexpectedEndTag =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1026",
                () => Resources.ParseError_UnexpectedEndTag,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_UnexpectedEndTag(SourceSpan location, string tagName)
        {
            return RazorDiagnostic.Create(Parsing_UnexpectedEndTag, location, tagName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_ExpectedCloseBracketBeforeEOF =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1027",
                () => Resources.ParseError_Expected_CloseBracket_Before_EOF,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_ExpectedCloseBracketBeforeEOF(SourceSpan location, string openBrace, string closeBrace)
        {
            return RazorDiagnostic.Create(Parsing_ExpectedCloseBracketBeforeEOF, location, openBrace, closeBrace);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_RazorCommentNotTerminated =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1028",
                () => Resources.ParseError_RazorComment_Not_Terminated,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_RazorCommentNotTerminated(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_RazorCommentNotTerminated, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_TagHelperIndexerAttributeNameMustIncludeKey =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1029",
                () => Resources.TagHelperBlockRewriter_IndexerAttributeNameMustIncludeKey,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_TagHelperIndexerAttributeNameMustIncludeKey(SourceSpan location, string attributeName, string tagName)
        {
            return RazorDiagnostic.Create(Parsing_TagHelperIndexerAttributeNameMustIncludeKey, location, attributeName, tagName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_TagHelperAttributeListMustBeWellFormed =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1030",
                () => Resources.TagHelperBlockRewriter_TagHelperAttributeListMustBeWellFormed,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_TagHelperAttributeListMustBeWellFormed(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_TagHelperAttributeListMustBeWellFormed, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_TagHelpersCannotHaveCSharpInTagDeclaration =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1031",
                () => Resources.TagHelpers_CannotHaveCSharpInTagDeclaration,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_TagHelpersCannotHaveCSharpInTagDeclaration(SourceSpan location, string tagName)
        {
            return RazorDiagnostic.Create(Parsing_TagHelpersCannotHaveCSharpInTagDeclaration, location, tagName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_TagHelperAttributesMustHaveAName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1032",
                () => Resources.TagHelpers_AttributesMustHaveAName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_TagHelperAttributesMustHaveAName(SourceSpan location, string tagName)
        {
            return RazorDiagnostic.Create(Parsing_TagHelperAttributesMustHaveAName, location, tagName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_TagHelperMustNotHaveAnEndTag =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1033",
                () => Resources.TagHelperParseTreeRewriter_EndTagTagHelperMustNotHaveAnEndTag,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_TagHelperMustNotHaveAnEndTag(SourceSpan location, string tagName, string displayName, TagStructure tagStructure)
        {
            var diagnostic = RazorDiagnostic.Create(
                Parsing_TagHelperMustNotHaveAnEndTag,
                location,
                tagName,
                displayName,
                tagStructure);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_TagHelperFoundMalformedTagHelper =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1034",
                () => Resources.TagHelpersParseTreeRewriter_FoundMalformedTagHelper,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_TagHelperFoundMalformedTagHelper(SourceSpan location, string tagName)
        {
            var diagnostic = RazorDiagnostic.Create(
                Parsing_TagHelperFoundMalformedTagHelper,
                location,
                tagName);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_TagHelperMissingCloseAngle =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1035",
                () => Resources.TagHelpersParseTreeRewriter_MissingCloseAngle,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_TagHelperMissingCloseAngle(SourceSpan location, string tagName)
        {
            var diagnostic = RazorDiagnostic.Create(
                Parsing_TagHelperMissingCloseAngle,
                location,
                tagName);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_InvalidTagHelperLookupText =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1036",
                () => Resources.InvalidTagHelperLookupText,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_InvalidTagHelperLookupText(SourceSpan location, string lookupText)
        {
            return RazorDiagnostic.Create(Parsing_InvalidTagHelperLookupText, location, lookupText);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveExpectsCSharpAttribute =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1037",
                () => Resources.DirectiveExpectsCSharpAttribute,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_DirectiveExpectsCSharpAttribute(SourceSpan location, string directiveName)
        {
            return RazorDiagnostic.Create(Parsing_DirectiveExpectsCSharpAttribute, location, directiveName);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveExpectsBooleanLiteral =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}1038",
                () => Resources.DirectiveExpectsBooleanLiteral,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_DirectiveExpectsBooleanLiteral(SourceSpan location, string directiveName)
        {
            return RazorDiagnostic.Create(Parsing_DirectiveExpectsBooleanLiteral, location, directiveName);
        }
        #endregion

        #region Semantic Errors

        // Semantic Errors ID Offset = 2000

        internal static readonly RazorDiagnosticDescriptor CodeTarget_UnsupportedExtension =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2000",
                () => Resources.Diagnostic_CodeTarget_UnsupportedExtension,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateCodeTarget_UnsupportedExtension(string documentKind, Type extensionType)
        {
            return RazorDiagnostic.Create(CodeTarget_UnsupportedExtension, SourceSpan.Undefined, documentKind, extensionType.Name);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_DuplicateDirective =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2001",
                () => Resources.DuplicateDirective,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_DuplicateDirective(SourceSpan location, string directive)
        {
            return RazorDiagnostic.Create(Parsing_DuplicateDirective, location, directive);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_SectionsCannotBeNested =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2002",
                () => Resources.ParseError_Sections_Cannot_Be_Nested,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_SectionsCannotBeNested(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_SectionsCannotBeNested, location, Resources.SectionExample);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_InlineMarkupBlocksCannotBeNested =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2003",
                () => Resources.ParseError_InlineMarkup_Blocks_Cannot_Be_Nested,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_InlineMarkupBlocksCannotBeNested(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_InlineMarkupBlocksCannotBeNested, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_NamespaceImportAndTypeAliasCannotExistWithinCodeBlock =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2004",
                () => Resources.ParseError_NamespaceImportAndTypeAlias_Cannot_Exist_Within_CodeBlock,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_NamespaceImportAndTypeAliasCannotExistWithinCodeBlock(SourceSpan location)
        {
            return RazorDiagnostic.Create(Parsing_NamespaceImportAndTypeAliasCannotExistWithinCodeBlock, location);
        }

        internal static readonly RazorDiagnosticDescriptor Parsing_DirectiveMustAppearAtStartOfLine =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2005",
                () => Resources.DirectiveMustAppearAtStartOfLine,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateParsing_DirectiveMustAppearAtStartOfLine(SourceSpan location, string directiveName)
        {
            return RazorDiagnostic.Create(Parsing_DirectiveMustAppearAtStartOfLine, location, directiveName);
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_CodeBlocksNotSupportedInAttributes =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2006",
                () => Resources.TagHelpers_CodeBlocks_NotSupported_InAttributes,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_CodeBlocksNotSupportedInAttributes(SourceSpan location)
        {
            var diagnostic = RazorDiagnostic.Create(TagHelper_CodeBlocksNotSupportedInAttributes, location);
            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InlineMarkupBlocksNotSupportedInAttributes =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2007",
                () => Resources.TagHelpers_InlineMarkupBlocks_NotSupported_InAttributes,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InlineMarkupBlocksNotSupportedInAttributes(SourceSpan location, string expectedTypeName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InlineMarkupBlocksNotSupportedInAttributes,
                location,
                expectedTypeName);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_EmptyBoundAttribute =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2008",
                () => Resources.RewriterError_EmptyTagHelperBoundAttribute,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_EmptyBoundAttribute(SourceSpan location, string attributeName, string tagName, string propertyTypeName)
        {
            return RazorDiagnostic.Create(TagHelper_EmptyBoundAttribute, location, attributeName, tagName, propertyTypeName);
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_CannotHaveNonTagContent =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2009",
                () => Resources.TagHelperParseTreeRewriter_CannotHaveNonTagContent,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_CannotHaveNonTagContent(SourceSpan location, string tagName, string allowedChildren)
        {
            return RazorDiagnostic.Create(TagHelper_CannotHaveNonTagContent, location, tagName, allowedChildren);
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidNestedTag =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2010",
                () => Resources.TagHelperParseTreeRewriter_InvalidNestedTag,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidNestedTag(SourceSpan location, string tagName, string parent, string allowedChildren)
        {
            return RazorDiagnostic.Create(TagHelper_InvalidNestedTag, location, tagName, parent, allowedChildren);
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InconsistentTagStructure =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}2011",
                () => Resources.TagHelperParseTreeRewriter_InconsistentTagStructure,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InconsistentTagStructure(SourceSpan location, string firstDescriptor, string secondDescriptor, string tagName)
        {
            return RazorDiagnostic.Create(TagHelper_InconsistentTagStructure, location, firstDescriptor, secondDescriptor, tagName, nameof(TagMatchingRuleDescriptor.TagStructure));
        }

        #endregion

        #region TagHelper Errors

        // TagHelper Errors ID Offset = 3000

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRestrictedChildNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3000",
                () => Resources.TagHelper_InvalidRestrictedChildNullOrWhitespace,
                RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic CreateTagHelper_InvalidRestrictedChildNullOrWhitespace(string tagHelperDisplayName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidRestrictedChildNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRestrictedChild =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3001",
                () => Resources.TagHelper_InvalidRestrictedChild,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidRestrictedChild(string tagHelperDisplayName, string restrictedChild, char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidRestrictedChild,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                restrictedChild,
                invalidCharacter);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3002",
                () => Resources.TagHelper_InvalidBoundAttributeNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeNullOrWhitespace(string tagHelperDisplayName, string propertyDisplayName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributeNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                propertyDisplayName);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3003",
                () => Resources.TagHelper_InvalidBoundAttributeName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeName(
            string tagHelperDisplayName,
            string propertyDisplayName,
            string invalidName,
            char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributeName,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                propertyDisplayName,
                invalidName,
                invalidCharacter);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeNameStartsWith =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3004",
                () => Resources.TagHelper_InvalidBoundAttributeNameStartsWith,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeNameStartsWith(
            string tagHelperDisplayName,
            string propertyDisplayName,
            string invalidName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributeNameStartsWith,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                propertyDisplayName,
                invalidName,
                "data-");

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributePrefix =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3005",
                () => Resources.TagHelper_InvalidBoundAttributePrefix,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributePrefix(
            string tagHelperDisplayName,
            string propertyDisplayName,
            string invalidName,
            char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributePrefix,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                propertyDisplayName,
                invalidName,
                invalidCharacter);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributePrefixStartsWith =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3006",
                () => Resources.TagHelper_InvalidBoundAttributePrefixStartsWith,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributePrefixStartsWith(
            string tagHelperDisplayName,
            string propertyDisplayName,
            string invalidName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributePrefixStartsWith,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                propertyDisplayName,
                invalidName,
                "data-");

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedTagNameNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3007",
                () => Resources.TagHelper_InvalidTargetedTagNameNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedTagNameNullOrWhitespace()
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedTagNameNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0));

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedTagName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3008",
                () => Resources.TagHelper_InvalidTargetedTagName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedTagName(string invalidTagName, char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedTagName,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                invalidTagName,
                invalidCharacter);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedParentTagNameNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3009",
                () => Resources.TagHelper_InvalidTargetedParentTagNameNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedParentTagNameNullOrWhitespace()
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedParentTagNameNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0));

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedParentTagName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3010",
                () => Resources.TagHelper_InvalidTargetedParentTagName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedParentTagName(string invalidTagName, char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedParentTagName,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                invalidTagName,
                invalidCharacter);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedAttributeNameNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3011",
                () => Resources.TagHelper_InvalidTargetedAttributeNameNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedAttributeNameNullOrWhitespace()
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedAttributeNameNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0));

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidTargetedAttributeName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3012",
                () => Resources.TagHelper_InvalidTargetedAttributeName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidTargetedAttributeName(string invalidAttributeName, char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidTargetedAttributeName,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                invalidAttributeName,
                invalidCharacter);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeParameterNullOrWhitespace =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3013",
                () => Resources.TagHelper_InvalidBoundAttributeParameterNullOrWhitespace,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeParameterNullOrWhitespace(string attributeName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributeParameterNullOrWhitespace,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                attributeName);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundAttributeParameterName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3014",
                () => Resources.TagHelper_InvalidBoundAttributeParameterName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundAttributeParameterName(
            string attributeName,
            string invalidName,
            char invalidCharacter)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundAttributeParameterName,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                attributeName,
                invalidName,
                invalidCharacter);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundDirectiveAttributeName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3015",
                () => Resources.TagHelper_InvalidBoundDirectiveAttributeName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundDirectiveAttributeName(
            string tagHelperDisplayName,
            string propertyDisplayName,
            string invalidName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundDirectiveAttributeName,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                propertyDisplayName,
                invalidName);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidBoundDirectiveAttributePrefix =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3015",
                () => Resources.TagHelper_InvalidBoundDirectiveAttributePrefix,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidBoundDirectiveAttributePrefix(
            string tagHelperDisplayName,
            string propertyDisplayName,
            string invalidName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidBoundDirectiveAttributePrefix,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                tagHelperDisplayName,
                propertyDisplayName,
                invalidName);

            return diagnostic;
        }

        internal static readonly RazorDiagnosticDescriptor TagHelper_InvalidRequiredDirectiveAttributeName =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}3016",
                () => Resources.TagHelper_InvalidRequiredDirectiveAttributeName,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateTagHelper_InvalidRequiredDirectiveAttributeName(
            string propertyDisplayName,
            string invalidName)
        {
            var diagnostic = RazorDiagnostic.Create(
                TagHelper_InvalidRequiredDirectiveAttributeName,
                new SourceSpan(SourceLocation.Undefined, contentLength: 0),
                propertyDisplayName,
                invalidName);

            return diagnostic;
        }

        #endregion

        #region Rewriter Errors

        // Rewriter Errors ID Offset = 4000

        internal static readonly RazorDiagnosticDescriptor Rewriter_InsufficientStack =
            new RazorDiagnosticDescriptor(
                $"{DiagnosticPrefix}4000",
                () => Resources.Rewriter_InsufficientStack,
                RazorDiagnosticSeverity.Error);
        public static RazorDiagnostic CreateRewriter_InsufficientStack(SourceSpan location)
        {
            return RazorDiagnostic.Create(Rewriter_InsufficientStack, location);
        }

        #endregion
    }
}
