// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class DesignTimeTagHelperWriter : TagHelperWriter
    {
        public string CreateTagHelperMethodName { get; set; } = "CreateTagHelper";

        public override void WriteDeclareTagHelperFields(CSharpRenderingContext context, DeclareTagHelperFieldsIRNode node)
        {
            foreach (var tagHelperTypeName in node.UsedTagHelperTypeNames)
            {
                var tagHelperVariableName = GetTagHelperVariableName(tagHelperTypeName);
                context.Writer
                    .Write("private global::")
                    .WriteVariableDeclaration(
                        tagHelperTypeName,
                        tagHelperVariableName,
                        value: null);
            }
        }

        public override void WriteAddTagHelperHtmlAttribute(CSharpRenderingContext context, AddTagHelperHtmlAttributeIRNode node)
        {
            throw new NotImplementedException();
        }

        public override void WriteCreateTagHelper(CSharpRenderingContext context, CreateTagHelperIRNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);

            context.Writer
                .WriteStartAssignment(tagHelperVariableName)
                .WriteStartMethodInvocation(
                    CreateTagHelperMethodName,
                    "global::" + node.TagHelperTypeName)
                .WriteEndMethodInvocation();
        }

        public override void WriteExecuteTagHelpers(CSharpRenderingContext context, ExecuteTagHelpersIRNode node)
        {
            // Do nothing
        }

        public override void WriteInitializeTagHelperStructure(CSharpRenderingContext context, InitializeTagHelperStructureIRNode node)
        {
            context.RenderChildren(node);
        }

        public override void WriteSetTagHelperProperty(CSharpRenderingContext context, SetTagHelperPropertyIRNode node)
        {
            throw new NotImplementedException();
        }

        private static string GetTagHelperVariableName(string tagHelperTypeName) => "__" + tagHelperTypeName.Replace('.', '_');
    }
}
