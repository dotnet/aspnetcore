// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public abstract class RazorIRNodeVisitor 
    {
        public virtual void Visit(RazorIRNode node)
        {
            node.Accept(this);
        }

        public virtual void VisitDefault(RazorIRNode node)
        {
        }

        public virtual void VisitDirectiveToken(DirectiveTokenIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitTemplate(TemplateIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitSection(SectionIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitCSharpStatement(CSharpStatementIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitCSharpExpression(CSharpExpressionIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitCSharpToken(CSharpTokenIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitHtmlAttributeValue(HtmlAttributeValueIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitCSharpAttributeValue(CSharpAttributeValueIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitHtmlAttribute(HtmlAttributeIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitSingleLineDirective(SingleLineDirectiveIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitBlockDirective(BlockDirectiveIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitClass(ClassDeclarationIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitMethodDeclaration(MethodDeclarationIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitDocument(DocumentIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitHtml(HtmlContentIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitNamespace(NamespaceDeclarationIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitUsingStatement(UsingStatementIRNode node)
        {
            VisitDefault(node);
        }
    }
}
