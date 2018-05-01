// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal static class BlazorDiagnosticFactory
    {
        public static readonly RazorDiagnosticDescriptor CodeBlockInAttribute =
            new RazorDiagnosticDescriptor(
            "BL9979",
            () =>
                "Code blocks delimited by '@{...}' like '@{{ {0} }}' for attributes are no longer supported " +
                "These features have been changed to use attribute syntax. " +
                "Use 'attr=\"@(x => {... }\"'.",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_CodeBlockInAttribute(SourceSpan? source, string expression)
        {
            var diagnostic = RazorDiagnostic.Create(
                CodeBlockInAttribute,
                source ?? SourceSpan.Undefined,
                expression);
            return diagnostic;
        }

        public static readonly RazorDiagnosticDescriptor ExpressionInAttributeList =
            new RazorDiagnosticDescriptor(
            "BL9980",
            () =>
                "Expressions like '{0}' inside of a tag must be part of an attribute. " +
                "Previous releases of Blazor supported constructs like '@onclick(...)' or '@bind(...)'." +
                "These features have been changed to use attribute syntax. " +
                "Use 'onclick=\"@...\"' or 'bind=\"...\" respectively.",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_ExpressionInAttributeList(SourceSpan? source, string expression)
        {
            var diagnostic = RazorDiagnostic.Create(
                ExpressionInAttributeList,
                source ?? SourceSpan.Undefined,
                expression);
            return diagnostic;
        }

        public static readonly RazorDiagnosticDescriptor UnexpectedClosingTag = new RazorDiagnosticDescriptor(
            "BL9981",
            () => "Unexpected closing tag '{0}' with no matching start tag.",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_UnexpectedClosingTag(SourceSpan? span, string tagName)
        {
            return RazorDiagnostic.Create(UnexpectedClosingTag, span ?? SourceSpan.Undefined, tagName);
        }

        public static readonly RazorDiagnosticDescriptor MismatchedClosingTag = new RazorDiagnosticDescriptor(
            "BL9982",
            () => "Mismatching closing tag. Found '{0}' but expected '{1}'.",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_MismatchedClosingTag(SourceSpan? span, string expectedTagName, string tagName)
        {
            return RazorDiagnostic.Create(MismatchedClosingTag, span ?? SourceSpan.Undefined, expectedTagName, tagName);
        }

        public static readonly RazorDiagnosticDescriptor MismatchedClosingTagKind = new RazorDiagnosticDescriptor(
            "BL9984",
            () => "Mismatching closing tag. Found '{0}' of type '{1}' but expected type '{2}'.",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_MismatchedClosingTagKind(SourceSpan? span, string tagName, string kind, string expectedKind)
        {
            return RazorDiagnostic.Create(MismatchedClosingTagKind, span ?? SourceSpan.Undefined, tagName, kind, expectedKind);
        }

        public static readonly RazorDiagnosticDescriptor MultipleComponents = new RazorDiagnosticDescriptor(
            "BL9985",
            () => "Multiple components use the tag '{0}'. Components: {1}",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_MultipleComponents(SourceSpan? span, string tagName, IEnumerable<TagHelperDescriptor> components)
        {
            return RazorDiagnostic.Create(MultipleComponents, span ?? SourceSpan.Undefined, tagName, string.Join(", ", components.Select(c => c.DisplayName)));
        }

        public static readonly RazorDiagnosticDescriptor UnsupportedComplexContent = new RazorDiagnosticDescriptor(
            "BL9986",
            () => "Component attributes do not support complex content (mixed C# and markup). Attribute: '{0}', text '{1}'",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_UnsupportedComplexContent(IntermediateNode node, string attributeName)
        {
            var content = string.Join("", node.FindDescendantNodes<IntermediateToken>().Select(t => t.Content));
            return RazorDiagnostic.Create(UnsupportedComplexContent, node.Source ?? SourceSpan.Undefined, attributeName, content);
        }

        public static readonly RazorDiagnosticDescriptor PageDirective_CannotBeImported =
            new RazorDiagnosticDescriptor(
            "BL9987",
            () => Resources.PageDirectiveCannotBeImported,
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic CreatePageDirective_CannotBeImported(SourceSpan source)
        {
            var fileName = Path.GetFileName(source.FilePath);
            var diagnostic = RazorDiagnostic.Create(PageDirective_CannotBeImported, source, PageDirective.Directive.Directive, fileName);

            return diagnostic;
        }

        public static readonly RazorDiagnosticDescriptor PageDirective_MustSpecifyRoute =
            new RazorDiagnosticDescriptor(
            "BL9988",
            () => "The @page directive must specify a route template. The route template must be enclosed in quotes and begin with the '/' character.",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic CreatePageDirective_MustSpecifyRoute(SourceSpan? source)
        {
            var diagnostic = RazorDiagnostic.Create(PageDirective_MustSpecifyRoute, source ?? SourceSpan.Undefined);
            return diagnostic;
        }

        public static readonly RazorDiagnosticDescriptor BindAttribute_Duplicates =
            new RazorDiagnosticDescriptor(
            "BL9989",
            () => "The attribute '{0}' was matched by multiple bind attributes. Duplicates:{1}",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic CreateBindAttribute_Duplicates(SourceSpan? source, string attribute, ComponentAttributeExtensionNode[] attributes)
        {
            var diagnostic = RazorDiagnostic.Create(
                BindAttribute_Duplicates,
                source ?? SourceSpan.Undefined,
                attribute,
                Environment.NewLine + string.Join(Environment.NewLine, attributes.Select(p => p.TagHelper.DisplayName)));
            return diagnostic;
        }

        public static readonly RazorDiagnosticDescriptor EventHandler_Duplicates =
            new RazorDiagnosticDescriptor(
            "BL9990",
            () => "The attribute '{0}' was matched by multiple event handlers attributes. Duplicates:{1}",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic CreateEventHandler_Duplicates(SourceSpan? source, string attribute, ComponentAttributeExtensionNode[] attributes)
        {
            var diagnostic = RazorDiagnostic.Create(
                EventHandler_Duplicates,
                source ?? SourceSpan.Undefined,
                attribute,
                Environment.NewLine + string.Join(Environment.NewLine, attributes.Select(p => p.TagHelper.DisplayName)));
            return diagnostic;
        }

        public static readonly RazorDiagnosticDescriptor BindAttribute_InvalidSyntax =
            new RazorDiagnosticDescriptor(
            "BL9991",
            () => "The attribute names could not be inferred from bind attibute '{0}'. Bind attributes should be of the form" +
                    "'bind', 'bind-value' or 'bind-value-change'",
            RazorDiagnosticSeverity.Error);

        public static readonly RazorDiagnosticDescriptor DisallowedScriptTag = new RazorDiagnosticDescriptor(
            "BL9992",
            () => "Script tags should not be placed inside components because they cannot be updated dynamically. To fix this, move the script tag to the 'index.html' file or another static location. For more information see https://go.microsoft.com/fwlink/?linkid=872131",
            RazorDiagnosticSeverity.Error);

        // Reserved: BL9993 Component parameters should not be public

        public static RazorDiagnostic Create_DisallowedScriptTag(SourceSpan? source)
        {
            var diagnostic = RazorDiagnostic.Create(DisallowedScriptTag, source ?? SourceSpan.Undefined);
            return diagnostic;
        }

        public static RazorDiagnostic CreateBindAttribute_InvalidSyntax(SourceSpan? source, string attribute)
        {
            var diagnostic = RazorDiagnostic.Create(
                BindAttribute_InvalidSyntax,
                source ?? SourceSpan.Undefined,
                attribute);
            return diagnostic;
        }
    }
}
