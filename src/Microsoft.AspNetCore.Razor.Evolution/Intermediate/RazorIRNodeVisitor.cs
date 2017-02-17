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

        public virtual void VisitChecksum(ChecksumIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitToken(RazorIRToken node)
        {
            VisitDefault(node);
        }

        public virtual void VisitDirectiveToken(DirectiveTokenIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitDirective(DirectiveIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitExtension(ExtensionIRNode node)
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

        public virtual void VisitClass(ClassDeclarationIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitRazorMethodDeclaration(RazorMethodDeclarationIRNode node)
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

        public virtual void VisitDeclareTagHelperFields(DeclareTagHelperFieldsIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitTagHelper(TagHelperIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitInitializeTagHelperStructure(InitializeTagHelperStructureIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitCreateTagHelper(CreateTagHelperIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitSetTagHelperProperty(SetTagHelperPropertyIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitAddTagHelperHtmlAttribute(AddTagHelperHtmlAttributeIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitExecuteTagHelpers(ExecuteTagHelpersIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitDeclarePreallocatedTagHelperHtmlAttribute(DeclarePreallocatedTagHelperHtmlAttributeIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitAddPreallocatedTagHelperHtmlAttribute(AddPreallocatedTagHelperHtmlAttributeIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitDeclarePreallocatedTagHelperAttribute(DeclarePreallocatedTagHelperAttributeIRNode node)
        {
            VisitDefault(node);
        }

        public virtual void VisitSetPreallocatedTagHelperProperty(SetPreallocatedTagHelperPropertyIRNode node)
        {
            VisitDefault(node);
        }
    }
}
