// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.CodeDom;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Generator
{
    public class RazorDirectiveAttributeCodeGenerator : SpanCodeGenerator
    {
        public RazorDirectiveAttributeCodeGenerator(string name, string value)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException(CommonResources.Argument_Cannot_Be_Null_Or_Empty, "name");
            }
            Name = name;
            Value = value ?? String.Empty; // Coerce to empty string if it was null.
        }

        public string Name { get; private set; }

        public string Value { get; private set; }

        public void GenerateCode(SyntaxTreeNode target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            if (Name == SyntaxConstants.CSharp.SessionStateKeyword)
            {
                codeTreeBuilder.AddSessionStateChunk(Value, target);
            }
        }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            var attributeType = new CodeTypeReference(typeof(RazorDirectiveAttribute));
            var attributeDeclaration = new CodeAttributeDeclaration(
                attributeType,
                new CodeAttributeArgument(new CodePrimitiveExpression(Name)),
                new CodeAttributeArgument(new CodePrimitiveExpression(Value)));
            context.GeneratedClass.CustomAttributes.Add(attributeDeclaration);
#endif
            // TODO: Make this generate the primary generator
            GenerateCode(target, context.CodeTreeBuilder, context);
        }

        public override string ToString()
        {
            return "Directive: " + Name + ", Value: " + Value;
        }

        public override bool Equals(object obj)
        {
            RazorDirectiveAttributeCodeGenerator other = obj as RazorDirectiveAttributeCodeGenerator;
            return other != null &&
                   Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) &&
                   Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Tuple.Create(Name.ToUpperInvariant(), Value.ToUpperInvariant())
                .GetHashCode();
        }
    }
}
