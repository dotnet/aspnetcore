// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
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

        public virtual void VisitTagHelperHtmlAttribute(TagHelperHtmlAttributeIntermediateNode node)
        {
            VisitDefault(node);
        }
    }
}
