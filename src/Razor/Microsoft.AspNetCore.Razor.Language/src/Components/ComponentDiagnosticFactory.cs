// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal static class ComponentDiagnosticFactory
{
    private const string DiagnosticPrefix = "RZ";

    public static readonly RazorDiagnosticDescriptor UnsupportedTagHelperDirective = new RazorDiagnosticDescriptor(
        $"{DiagnosticPrefix}9978",
        () =>
            "The directives @addTagHelper, @removeTagHelper and @tagHelperPrefix are not valid in a component document. " +
            "Use '@using <namespace>' directive instead.",
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_UnsupportedTagHelperDirective(SourceSpan? source)
    {
        return RazorDiagnostic.Create(UnsupportedTagHelperDirective, source ?? SourceSpan.Undefined);
    }

    public static readonly RazorDiagnosticDescriptor CodeBlockInAttribute = new RazorDiagnosticDescriptor(
        $"{DiagnosticPrefix}9979",
        () =>
            "Code blocks delimited by '@{{...}}' like '@{{ {0} }}' for attributes are no longer supported " +
            "These features have been changed to use attribute syntax. " +
            "Use 'attr=\"@(x => {{... }}\"'.",
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
        $"{DiagnosticPrefix}9980",
        () => "Unclosed tag '{0}' with no matching end tag.",
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_UnclosedTag(SourceSpan? span, string tagName)
    {
        return RazorDiagnostic.Create(UnclosedTag, span ?? SourceSpan.Undefined, tagName);
    }

    public static readonly RazorDiagnosticDescriptor UnexpectedClosingTag = new RazorDiagnosticDescriptor(
        $"{DiagnosticPrefix}9981",
        () => "Unexpected closing tag '{0}' with no matching start tag.",
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_UnexpectedClosingTag(SourceSpan? span, string tagName)
    {
        return RazorDiagnostic.Create(UnexpectedClosingTag, span ?? SourceSpan.Undefined, tagName);
    }

    public static readonly RazorDiagnosticDescriptor UnexpectedClosingTagForVoidElement = new RazorDiagnosticDescriptor(
        $"{DiagnosticPrefix}9983",
        () => "Unexpected closing tag '{0}'. The element '{0}' is a void element, and should be used without a closing tag.",
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_UnexpectedClosingTagForVoidElement(SourceSpan? span, string tagName)
    {
        return RazorDiagnostic.Create(UnexpectedClosingTagForVoidElement, span ?? SourceSpan.Undefined, tagName);
    }

    public static readonly RazorDiagnosticDescriptor InvalidHtmlContent = new RazorDiagnosticDescriptor(
        $"{DiagnosticPrefix}9984",
        () => "Found invalid HTML content. Text '{0}'",
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_InvalidHtmlContent(SourceSpan? span, string text)
    {
        return RazorDiagnostic.Create(InvalidHtmlContent, span ?? SourceSpan.Undefined, text);
    }

    public static readonly RazorDiagnosticDescriptor MultipleComponents = new RazorDiagnosticDescriptor(
        $"{DiagnosticPrefix}9985",
        () => "Multiple components use the tag '{0}'. Components: {1}",
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_MultipleComponents(SourceSpan? span, string tagName, IEnumerable<TagHelperDescriptor> components)
    {
        return RazorDiagnostic.Create(MultipleComponents, span ?? SourceSpan.Undefined, tagName, string.Join(", ", components.Select(c => c.DisplayName)));
    }

    public static readonly RazorDiagnosticDescriptor UnsupportedComplexContent = new RazorDiagnosticDescriptor(
        $"{DiagnosticPrefix}9986",
        () => "Component attributes do not support complex content (mixed C# and markup). Attribute: '{0}', text: '{1}'",
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_UnsupportedComplexContent(IntermediateNode node, string attributeName)
    {
        var content = string.Join("", node.FindDescendantNodes<IntermediateToken>().Select(t => t.Content));
        return RazorDiagnostic.Create(UnsupportedComplexContent, node.Source ?? SourceSpan.Undefined, attributeName, content);
    }

    public static readonly RazorDiagnosticDescriptor PageDirective_CannotBeImported =
        new RazorDiagnosticDescriptor(
        $"{DiagnosticPrefix}9987",
        () => ComponentResources.PageDirectiveCannotBeImported,
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreatePageDirective_CannotBeImported(SourceSpan source)
    {
        var fileName = Path.GetFileName(source.FilePath);
        var diagnostic = RazorDiagnostic.Create(PageDirective_CannotBeImported, source, "page", fileName);

        return diagnostic;
    }

    public static readonly RazorDiagnosticDescriptor PageDirective_MustSpecifyRoute =
        new RazorDiagnosticDescriptor(
        $"{DiagnosticPrefix}9988",
        () => "The @page directive must specify a route template. The route template must be enclosed in quotes and begin with the '/' character.",
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreatePageDirective_MustSpecifyRoute(SourceSpan? source)
    {
        var diagnostic = RazorDiagnostic.Create(PageDirective_MustSpecifyRoute, source ?? SourceSpan.Undefined);
        return diagnostic;
    }

    public static readonly RazorDiagnosticDescriptor BindAttribute_Duplicates =
        new RazorDiagnosticDescriptor(
        $"{DiagnosticPrefix}9989",
        () => "The attribute '{0}' was matched by multiple bind attributes. Duplicates:{1}",
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateBindAttribute_Duplicates(SourceSpan? source, string attribute, TagHelperDirectiveAttributeIntermediateNode[] attributes)
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
        $"{DiagnosticPrefix}9990",
        () => "The attribute '{0}' was matched by multiple event handler attributes. Duplicates:{1}",
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateEventHandler_Duplicates(SourceSpan? source, string attribute, TagHelperDirectiveAttributeIntermediateNode[] attributes)
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
        $"{DiagnosticPrefix}9991",
        () => "The attribute names could not be inferred from bind attribute '{0}'. Bind attributes should be of the form " +
            "'bind' or 'bind-value' along with their corresponding optional parameters like 'bind-value:event', 'bind:format' etc.",
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
        $"{DiagnosticPrefix}9992",
        () => "Script tags should not be placed inside components because they cannot be updated dynamically. To fix this, move the script tag to the 'index.html' file or another static location. For more information, see https://aka.ms/AAe3qu3",
        RazorDiagnosticSeverity.Error);

    // Reserved: BL9993 Component parameters should not be public

    public static RazorDiagnostic Create_DisallowedScriptTag(SourceSpan? source)
    {
        var diagnostic = RazorDiagnostic.Create(DisallowedScriptTag, source ?? SourceSpan.Undefined);
        return diagnostic;
    }

    public static readonly RazorDiagnosticDescriptor TemplateInvalidLocation =
        new RazorDiagnosticDescriptor(
        $"{DiagnosticPrefix}9994",
        () => "Razor templates cannot be used in attributes.",
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_TemplateInvalidLocation(SourceSpan? source)
    {
        return RazorDiagnostic.Create(TemplateInvalidLocation, source ?? SourceSpan.Undefined);
    }

    public static readonly RazorDiagnosticDescriptor ChildContentSetByAttributeAndBody =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}9995",
            () => "The child content property '{0}' is set by both the attribute and the element contents.",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_ChildContentSetByAttributeAndBody(SourceSpan? source, string attribute)
    {
        return RazorDiagnostic.Create(ChildContentSetByAttributeAndBody, source ?? SourceSpan.Undefined, attribute);
    }

    public static readonly RazorDiagnosticDescriptor ChildContentMixedWithExplicitChildContent =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}9996",
            () => "Unrecognized child content inside component '{0}'. The component '{0}' accepts child content through the " +
            "following top-level items: {1}.",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_ChildContentMixedWithExplicitChildContent(SourceSpan? source, ComponentIntermediateNode component)
    {
        var supportedElements = string.Join(", ", component.Component.GetChildContentProperties().Select(p => $"'{p.Name}'"));
        return RazorDiagnostic.Create(ChildContentMixedWithExplicitChildContent, source ?? SourceSpan.Undefined, component.TagName, supportedElements);
    }

    public static readonly RazorDiagnosticDescriptor ChildContentHasInvalidAttribute =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}9997",
            () => "Unrecognized attribute '{0}' on child content element '{1}'.",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_ChildContentHasInvalidAttribute(SourceSpan? source, string attribute, string element)
    {
        return RazorDiagnostic.Create(ChildContentHasInvalidAttribute, source ?? SourceSpan.Undefined, attribute, element);
    }

    public static readonly RazorDiagnosticDescriptor ChildContentHasInvalidParameter =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}9998",
            () => "Invalid parameter name. The parameter name attribute '{0}' on child content element '{1}' can only include literal text.",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_ChildContentHasInvalidParameter(SourceSpan? source, string attribute, string element)
    {
        return RazorDiagnostic.Create(ChildContentHasInvalidParameter, source ?? SourceSpan.Undefined, attribute, element);
    }

    public static readonly RazorDiagnosticDescriptor ChildContentRepeatedParameterName =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}9999",
            () => "The child content element '{0}' of component '{1}' uses the same parameter name ('{2}') as enclosing child content " +
            "element '{3}' of component '{4}'. Specify the parameter name like: '<{0} Context=\"another_name\"> to resolve the ambiguity",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_ChildContentRepeatedParameterName(
        SourceSpan? source,
        ComponentChildContentIntermediateNode childContent1,
        ComponentIntermediateNode component1,
        ComponentChildContentIntermediateNode childContent2,
        ComponentIntermediateNode component2)
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
            $"{DiagnosticPrefix}10000",
            () => "The component '{0}' is missing required type arguments. Specify the missing types using the attributes: {1}.",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_GenericComponentMissingTypeArgument(
        SourceSpan? source,
        ComponentIntermediateNode component,
        IEnumerable<BoundAttributeDescriptor> attributes)
    {
        Debug.Assert(component.Component.IsGenericTypedComponent());

        var attributesText = string.Join(", ", attributes.Select(a => $"'{a.Name}'"));
        return RazorDiagnostic.Create(GenericComponentMissingTypeArgument, source ?? SourceSpan.Undefined, component.TagName, attributesText);
    }

    public static readonly RazorDiagnosticDescriptor GenericComponentTypeInferenceUnderspecified =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}10001",
            () => "The type of component '{0}' cannot be inferred based on the values provided. Consider specifying the type arguments " +
                "directly using the following attributes: {1}.",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_GenericComponentTypeInferenceUnderspecified(
        SourceSpan? source,
        ComponentIntermediateNode component,
        IEnumerable<BoundAttributeDescriptor> attributes)
    {
        Debug.Assert(component.Component.IsGenericTypedComponent());

        var attributesText = string.Join(", ", attributes.Select(a => $"'{a.Name}'"));
        return RazorDiagnostic.Create(GenericComponentTypeInferenceUnderspecified, source ?? SourceSpan.Undefined, component.TagName, attributesText);
    }

    public static readonly RazorDiagnosticDescriptor ChildContentHasInvalidParameterOnComponent =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}10002",
            () => "Invalid parameter name. The parameter name attribute '{0}' on component '{1}' can only include literal text.",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_ChildContentHasInvalidParameterOnComponent(SourceSpan? source, string attribute, string element)
    {
        return RazorDiagnostic.Create(ChildContentHasInvalidParameterOnComponent, source ?? SourceSpan.Undefined, attribute, element);
    }

    public static readonly RazorDiagnosticDescriptor UnsupportedComponentImportContent =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}10003",
            () => "Markup, code and block directives are not valid in component imports.",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_UnsupportedComponentImportContent(SourceSpan? source)
    {
        return RazorDiagnostic.Create(UnsupportedComponentImportContent, source ?? SourceSpan.Undefined);
    }

    public static readonly RazorDiagnosticDescriptor BindAttributeParameter_MissingBind =
        new RazorDiagnosticDescriptor(
        $"{DiagnosticPrefix}10004",
        () => "Could not find the non-parameterized bind attribute that corresponds to the attribute '{0}'.",
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateBindAttributeParameter_MissingBind(SourceSpan? source, string attribute)
    {
        var diagnostic = RazorDiagnostic.Create(
            BindAttributeParameter_MissingBind,
            source ?? SourceSpan.Undefined,
            attribute);
        return diagnostic;
    }

    public static readonly RazorDiagnosticDescriptor DuplicateMarkupAttribute =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}10007",
            () => "The attribute '{0}' is used two or more times for this element. Attributes must be unique (case-insensitive).",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_DuplicateMarkupAttribute(string attributeName, SourceSpan? source = null)
    {
        return RazorDiagnostic.Create(DuplicateMarkupAttribute, source ?? SourceSpan.Undefined, attributeName);
    }

    public static readonly RazorDiagnosticDescriptor DuplicateMarkupAttributeDirective =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}10008",
            () =>
                "The attribute '{0}' is used two or more times for this element. Attributes must be unique (case-insensitive). " +
                "The attribute '{0}' is used by the '{1}' directive attribute.",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_DuplicateMarkupAttributeDirective(string attributeName, string directiveAttributeName, SourceSpan? source = null)
    {
        return RazorDiagnostic.Create(DuplicateMarkupAttributeDirective, source ?? SourceSpan.Undefined, attributeName, directiveAttributeName);
    }

    public static readonly RazorDiagnosticDescriptor DuplicateComponentParameter =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}10009",
            () => "The component parameter '{0}' is used two or more times for this component. Parameters must be unique (case-insensitive).",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_DuplicateComponentParameter(string attributeName, SourceSpan? source = null)
    {
        return RazorDiagnostic.Create(DuplicateComponentParameter, source ?? SourceSpan.Undefined, attributeName);
    }

    public static readonly RazorDiagnosticDescriptor DuplicateComponentParameterDirective =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}10010",
            () =>
                "The component parameter '{0}' is used two or more times for this component. Parameters must be unique (case-insensitive). " +
                "The component parameter '{0}' is generated by the '{1}' directive attribute.",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_DuplicateComponentParameterDirective(string attributeName, string directiveAttributeName, SourceSpan? source = null)
    {
        return RazorDiagnostic.Create(DuplicateComponentParameterDirective, source ?? SourceSpan.Undefined, attributeName, directiveAttributeName);
    }

    public static readonly RazorDiagnosticDescriptor ComponentNamesCannotStartWithLowerCase =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}10011",
            () => "Component '{0}' starts with a lowercase character. Component names cannot start with a lowercase character.",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_ComponentNamesCannotStartWithLowerCase(string componentName, SourceSpan? source = null)
    {
        return RazorDiagnostic.Create(
            ComponentNamesCannotStartWithLowerCase,
            source ?? SourceSpan.Undefined,
            componentName);
    }

    public static readonly RazorDiagnosticDescriptor UnexpectedMarkupElement =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}10012",
            () => "Found markup element with unexpected name '{0}'. If this is intended to be a component, add a @using directive for its namespace.",
            RazorDiagnosticSeverity.Warning);

    public static RazorDiagnostic Create_UnexpectedMarkupElement(string elementName, SourceSpan? source = null)
    {
        return RazorDiagnostic.Create(
            UnexpectedMarkupElement,
            source ?? SourceSpan.Undefined,
            elementName);
    }

    public static readonly RazorDiagnosticDescriptor InconsistentStartAndEndTagName =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}10013",
            () => "The start tag name '{0}' does not match the end tag name '{1}'. Components must have matching start and end tag names (case-sensitive).",
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic Create_InconsistentStartAndEndTagName(string startTagName, string endTagName, SourceSpan? source = null)
    {
        return RazorDiagnostic.Create(
            InconsistentStartAndEndTagName,
            source ?? SourceSpan.Undefined,
            startTagName,
            endTagName);
    }

    public static readonly RazorDiagnosticDescriptor EventHandlerParameter_Duplicates =
        new RazorDiagnosticDescriptor(
        $"{DiagnosticPrefix}10014",
        () => "The attribute '{0}' was matched by multiple event handlers parameter attributes. Duplicates:{1}",
        RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateEventHandlerParameter_Duplicates(SourceSpan? source, string attribute, TagHelperDirectiveAttributeParameterIntermediateNode[] attributes)
    {
        var diagnostic = RazorDiagnostic.Create(
            EventHandlerParameter_Duplicates,
            source ?? SourceSpan.Undefined,
            attribute,
            Environment.NewLine + string.Join(Environment.NewLine, attributes.Select(p => p.TagHelper.DisplayName)));
        return diagnostic;
    }
}
