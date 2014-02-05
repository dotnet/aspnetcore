// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Generator
{
    public class AttributeBlockCodeGenerator : BlockCodeGenerator
    {
        public AttributeBlockCodeGenerator(string name, LocationTagged<string> prefix, LocationTagged<string> suffix)
        {
            Name = name;
            Prefix = prefix;
            Suffix = suffix;
        }

        public string Name { get; private set; }
        public LocationTagged<string> Prefix { get; private set; }
        public LocationTagged<string> Suffix { get; private set; }

        public void GenerateStartBlockCode(SyntaxTreeNode target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            CodeAttributeChunk chunk = codeTreeBuilder.StartChunkBlock<CodeAttributeChunk>(target);

            chunk.Attribute = Name;
            chunk.Prefix = Prefix;
            chunk.Suffix = Suffix;
        }

        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM in CoreCLR.
            // #if'd the entire section because once we transition over to the CodeTree we will not need all this code.

            if (context.Host.DesignTimeMode)
            {
                return; // Don't generate anything!
            }

            context.FlushBufferedStatement();
            context.AddStatement(context.BuildCodeString(cw =>
            {
                if (!String.IsNullOrEmpty(context.TargetWriterName))
                {
                    cw.WriteStartMethodInvoke(context.Host.GeneratedClassContext.WriteAttributeToMethodName);
                    cw.WriteSnippet(context.TargetWriterName);
                    cw.WriteParameterSeparator();
                }
                else
                {
                    cw.WriteStartMethodInvoke(context.Host.GeneratedClassContext.WriteAttributeMethodName);
                }
                cw.WriteStringLiteral(Name);
                cw.WriteParameterSeparator();
                cw.WriteLocationTaggedString(Prefix);
                cw.WriteParameterSeparator();
                cw.WriteLocationTaggedString(Suffix);
            }));
#endif

            // TODO: Make this generate the primary generator
            GenerateStartBlockCode(target, context.CodeTreeBuilder, context);
        }

        public void GenerateEndBlockCode(SyntaxTreeNode target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            codeTreeBuilder.EndChunkBlock();
        }

        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM in CoreCLR (AddStatement and FlushBufferedStatement etc. utilize it).
            // #if'd the entire section because once we transition over to the CodeTree we will not need all this code.

            if (context.Host.DesignTimeMode)
            {
                return; // Don't generate anything!
            }

            context.FlushBufferedStatement();
            context.AddStatement(context.BuildCodeString(cw =>
            {
                cw.WriteEndMethodInvoke();
                cw.WriteEndStatement();
            }));
#endif
            // TODO: Make this generate the primary generator
            GenerateEndBlockCode(target, context.CodeTreeBuilder, context);
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "Attr:{0},{1:F},{2:F}", Name, Prefix, Suffix);
        }

        public override bool Equals(object obj)
        {
            AttributeBlockCodeGenerator other = obj as AttributeBlockCodeGenerator;
            return other != null &&
                   String.Equals(other.Name, Name, StringComparison.Ordinal) &&
                   Equals(other.Prefix, Prefix) &&
                   Equals(other.Suffix, Suffix);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(Name)
                .Add(Prefix)
                .Add(Suffix)
                .CombinedHash;
        }
    }
}
