// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public abstract class IntermediateNodeVisitor
{
    public virtual void Visit(IntermediateNode node)
    {
        node.Accept(this);
    }

    public virtual void VisitDefault(IntermediateNode node)
    {
    }

    public virtual void VisitToken(IntermediateToken node)
    {
        VisitDefault(node);
    }

    public virtual void VisitDirectiveToken(DirectiveTokenIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitDirective(DirectiveIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitMalformedDirective(MalformedDirectiveIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitExtension(ExtensionIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitCSharpCode(CSharpCodeIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitCSharpExpression(CSharpExpressionIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitHtmlAttributeValue(HtmlAttributeValueIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitCSharpExpressionAttributeValue(CSharpExpressionAttributeValueIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitCSharpCodeAttributeValue(CSharpCodeAttributeValueIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitMethodDeclaration(MethodDeclarationIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitFieldDeclaration(FieldDeclarationIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitPropertyDeclaration(PropertyDeclarationIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitDocument(DocumentIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitHtml(HtmlContentIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitUsingDirective(UsingDirectiveIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitTagHelper(TagHelperIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitTagHelperBody(TagHelperBodyIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitTagHelperProperty(TagHelperPropertyIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitTagHelperDirectiveAttribute(TagHelperDirectiveAttributeIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitTagHelperHtmlAttribute(TagHelperHtmlAttributeIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitTagHelperDirectiveAttributeParameter(TagHelperDirectiveAttributeParameterIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitComponent(ComponentIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitComponentAttribute(ComponentAttributeIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitComponentChildContent(ComponentChildContentIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitComponentTypeArgument(ComponentTypeArgumentIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitComponentTypeInferenceMethod(ComponentTypeInferenceMethodIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitMarkupElement(MarkupElementIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitMarkupBlock(MarkupBlockIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitReferenceCapture(ReferenceCaptureIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitSetKey(SetKeyIntermediateNode node)
    {
        VisitDefault(node);
    }

    public virtual void VisitSplat(SplatIntermediateNode node)
    {
        VisitDefault(node);
    }
}
