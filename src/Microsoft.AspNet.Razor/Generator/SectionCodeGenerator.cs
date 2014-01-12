// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.Internal.Web.Utils;
using System;

namespace Microsoft.AspNet.Razor.Generator
{
    public class SectionCodeGenerator : BlockCodeGenerator
    {
        public SectionCodeGenerator(string sectionName)
        {
            SectionName = sectionName;
        }

        public string SectionName { get; private set; }

        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
            string startBlock = context.BuildCodeString(cw =>
            {
                cw.WriteStartMethodInvoke(context.Host.GeneratedClassContext.DefineSectionMethodName);
                cw.WriteStringLiteral(SectionName);
                cw.WriteParameterSeparator();
                cw.WriteStartLambdaDelegate();
            });
            context.AddStatement(startBlock);
        }

        public override void GenerateEndBlockCode(Block target, CodeGeneratorContext context)
        {
            string startBlock = context.BuildCodeString(cw =>
            {
                cw.WriteEndLambdaDelegate();
                cw.WriteEndMethodInvoke();
                cw.WriteEndStatement();
            });
            context.AddStatement(startBlock);
        }

        public override bool Equals(object obj)
        {
            SectionCodeGenerator other = obj as SectionCodeGenerator;
            return other != null &&
                   base.Equals(other) &&
                   String.Equals(SectionName, other.SectionName, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(base.GetHashCode())
                .Add(SectionName)
                .CombinedHash;
        }

        public override string ToString()
        {
            return "Section:" + SectionName;
        }
    }
}
