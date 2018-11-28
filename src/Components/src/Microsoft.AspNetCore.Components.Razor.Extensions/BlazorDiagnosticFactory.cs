// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
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

        public static readonly RazorDiagnosticDescriptor UnclosedTag = new RazorDiagnosticDescriptor(
            "BL9980",
            () => "Unclosed tag '{0}' with no matching end tag.",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_UnclosedTag(SourceSpan? span, string tagName)
        {
            return RazorDiagnostic.Create(UnclosedTag, span ?? SourceSpan.Undefined, tagName);
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

        public static readonly RazorDiagnosticDescriptor UnexpectedClosingTagForVoidElement = new RazorDiagnosticDescriptor(
            "BL9983",
            () => "Unexpected closing tag '{0}'. The element '{0}' is a void element, and should be used without a closing tag.",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_UnexpectedClosingTagForVoidElement(SourceSpan? span, string tagName)
        {
            return RazorDiagnostic.Create(UnexpectedClosingTagForVoidElement, span ?? SourceSpan.Undefined, tagName);
        }

        public static readonly RazorDiagnosticDescriptor InvalidHtmlContent = new RazorDiagnosticDescriptor(
            "BL9984",
            () => "Found invalid HTML content. Text '{0}'",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_InvalidHtmlContent(SourceSpan? span, string text)
        {
            return RazorDiagnostic.Create(InvalidHtmlContent, span ?? SourceSpan.Undefined, text);
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

        public static RazorDiagnostic CreateBindAttribute_Duplicates(SourceSpan? source, string attribute, TagHelperPropertyIntermediateNode[] attributes)
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

        public static RazorDiagnostic CreateEventHandler_Duplicates(SourceSpan? source, string attribute, TagHelperPropertyIntermediateNode[] attributes)
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
            () => "The attribute names could not be inferred from bind attribute '{0}'. Bind attributes should be of the form" +
                "'bind', 'bind-value' or 'bind-value-change'",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic CreateBindAttribute_InvalidSyntax(SourceSpan? source, string attribute)
        {
            var diagnostic = RazorDiagnostic.Create(
                BindAttribute_InvalidSyntax,
                source ?? SourceSpan.Undefined,
                attribute);
            return diagnostic;
        }

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

        public static readonly RazorDiagnosticDescriptor TemplateInvalidLocation =
            new RazorDiagnosticDescriptor(
            "BL9994",
            () => "Razor templates cannot be used in attributes.",
            RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_TemplateInvalidLocation(SourceSpan? source)
        {
            return RazorDiagnostic.Create(TemplateInvalidLocation, source ?? SourceSpan.Undefined);
        }

        public static readonly RazorDiagnosticDescriptor ChildContentSetByAttributeAndBody =
            new RazorDiagnosticDescriptor(
                "BL9995",
                () => "The child content property '{0}' is set by both the attribute and the element contents.",
                RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_ChildContentSetByAttributeAndBody(SourceSpan? source, string attribute)
        {
            return RazorDiagnostic.Create(ChildContentSetByAttributeAndBody, source ?? SourceSpan.Undefined, attribute);
        }

        public static readonly RazorDiagnosticDescriptor ChildContentMixedWithExplicitChildContent =
            new RazorDiagnosticDescriptor(
                "BL9996",
                () => "Unrecognized child content inside component '{0}'. The component '{0}' accepts child content through the " +
                "following top-level items: {1}.",
                RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_ChildContentMixedWithExplicitChildContent(SourceSpan? source, ComponentExtensionNode component)
        {
            var supportedElements = string.Join(", ", component.Component.GetChildContentProperties().Select(p => $"'{p.Name}'"));
            return RazorDiagnostic.Create(ChildContentMixedWithExplicitChildContent, source ?? SourceSpan.Undefined, component.TagName, supportedElements);
        }

        public static readonly RazorDiagnosticDescriptor ChildContentHasInvalidAttribute =
            new RazorDiagnosticDescriptor(
                "BL9997",
                () => "Unrecognized attribute '{0}' on child content element '{1}'.",
                RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_ChildContentHasInvalidAttribute(SourceSpan? source, string attribute, string element)
        {
            return RazorDiagnostic.Create(ChildContentHasInvalidAttribute, source ?? SourceSpan.Undefined, attribute, element);
        }

        public static readonly RazorDiagnosticDescriptor ChildContentHasInvalidParameter =
            new RazorDiagnosticDescriptor(
                "BL9998",
                () => "Invalid parameter name. The parameter name attribute '{0}' on child content element '{1}' can only include literal text.",
                RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_ChildContentHasInvalidParameter(SourceSpan? source, string attribute, string element)
        {
            return RazorDiagnostic.Create(ChildContentHasInvalidParameter, source ?? SourceSpan.Undefined, attribute, element);
        }

        public static readonly RazorDiagnosticDescriptor ChildContentRepeatedParameterName =
            new RazorDiagnosticDescriptor(
                "BL9999",
                () => "The child content element '{0}' of component '{1}' uses the same parameter name ('{2}') as enclosing child content " +
                "element '{3}' of component '{4}'. Specify the parameter name like: '<{0} Context=\"another_name\"> to resolve the ambiguity",
                RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_ChildContentRepeatedParameterName(
            SourceSpan? source,
            ComponentChildContentIntermediateNode childContent1,
            ComponentExtensionNode component1,
            ComponentChildContentIntermediateNode childContent2,
            ComponentExtensionNode component2)
        {
            Debug.Assert(childContent1.ParameterName == childContent2.ParameterName);
            Debug.Assert(childContent1.IsParameterized);
            Debug.Assert(childContent2.IsParameterized);

            return RazorDiagnostic.Create(
                ChildContentRepeatedParameterName,
                source ?? SourceSpan.Undefined,
                childContent1.AttributeName,
                component1.TagName,
                childContent1.ParameterName,
                childContent2.AttributeName,
                component2.TagName);
        }

        public static readonly RazorDiagnosticDescriptor GenericComponentMissingTypeArgument =
            new RazorDiagnosticDescriptor(
                "BL10000",
                () => "The component '{0}' is missing required type arguments. Specify the missing types using the attributes: {1}.",
                RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_GenericComponentMissingTypeArgument(
            SourceSpan? source,
            ComponentExtensionNode component,
            IEnumerable<BoundAttributeDescriptor> attributes)
        {
            Debug.Assert(component.Component.IsGenericTypedComponent());

            var attributesText = string.Join(", ", attributes.Select(a => $"'{a.Name}'"));
            return RazorDiagnostic.Create(GenericComponentMissingTypeArgument, source ?? SourceSpan.Undefined, component.TagName, attributesText);
        }

        public static readonly RazorDiagnosticDescriptor GenericComponentTypeInferenceUnderspecified =
            new RazorDiagnosticDescriptor(
                "BL10001",
                () => "The type of component '{0}' cannot be inferred based on the values provided. Consider specifying the type arguments " +
                    "directly using the following attributes: {1}.",
                RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_GenericComponentTypeInferenceUnderspecified(
            SourceSpan? source,
            ComponentExtensionNode component,
            IEnumerable<BoundAttributeDescriptor> attributes)
        {
            Debug.Assert(component.Component.IsGenericTypedComponent());

            var attributesText = string.Join(", ", attributes.Select(a => $"'{a.Name}'"));
            return RazorDiagnostic.Create(GenericComponentTypeInferenceUnderspecified, source ?? SourceSpan.Undefined, component.TagName, attributesText);
        }

        public static readonly RazorDiagnosticDescriptor ChildContentHasInvalidParameterOnComponent =
            new RazorDiagnosticDescriptor(
                "BL10002",
                () => "Invalid parameter name. The parameter name attribute '{0}' on component '{1}' can only include literal text.",
                RazorDiagnosticSeverity.Error);

        public static RazorDiagnostic Create_ChildContentHasInvalidParameterOnComponent(SourceSpan? source, string attribute, string element)
        {
            return RazorDiagnostic.Create(ChildContentHasInvalidParameterOnComponent, source ?? SourceSpan.Undefined, attribute, element);
        }
    }
}
