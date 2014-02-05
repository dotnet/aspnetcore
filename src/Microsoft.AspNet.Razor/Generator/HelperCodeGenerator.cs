// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.CodeDom;
using System.Globalization;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Generator
{
    public class HelperCodeGenerator : BlockCodeGenerator
    {
        private const string HelperWriterName = "__razor_helper_writer";

        private CodeWriter _writer;
        private string _oldWriter;
        private IDisposable _statementCollectorToken;

        public HelperCodeGenerator(LocationTagged<string> signature, bool headerComplete)
        {
            Signature = signature;
            HeaderComplete = headerComplete;
        }

        public LocationTagged<string> Signature { get; private set; }
        public LocationTagged<string> Footer { get; set; }
        public bool HeaderComplete { get; private set; }

        public void GenerateStartBlockCode(SyntaxTreeNode target, CodeTreeBuilder codeTreeBuilder, CodeGeneratorContext context)
        {
            HelperChunk chunk = codeTreeBuilder.StartChunkBlock<HelperChunk>(target, topLevel: true);

            chunk.Signature = Signature;
            chunk.Footer = Footer;
            chunk.HeaderComplete = HeaderComplete;
        }

        public override void GenerateStartBlockCode(Block target, CodeGeneratorContext context)
        {
#if NET45
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            _writer = context.CreateCodeWriter();

            string prefix = context.BuildCodeString(
                cw => cw.WriteHelperHeaderPrefix(context.Host.GeneratedClassContext.TemplateTypeName, context.Host.StaticHelpers));

            _writer.WriteLinePragma(
                context.GenerateLinePragma(Signature.Location, prefix.Length, Signature.Value.Length));
            _writer.WriteSnippet(prefix);
            _writer.WriteSnippet(Signature);
            if (HeaderComplete)
            {
                _writer.WriteHelperHeaderSuffix(context.Host.GeneratedClassContext.TemplateTypeName);
            }
            _writer.WriteLinePragma(null);
            if (HeaderComplete)
            {
                _writer.WriteReturn();
                _writer.WriteStartConstructor(context.Host.GeneratedClassContext.TemplateTypeName);
                _writer.WriteStartLambdaDelegate(HelperWriterName);
            }

            _statementCollectorToken = context.ChangeStatementCollector(AddStatementToHelper);
#endif
            _oldWriter = context.TargetWriterName;
            context.TargetWriterName = HelperWriterName;

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
            // No CodeDOM + This code will not be needed once we transition to the CodeTree

            _statementCollectorToken.Dispose();
            if (HeaderComplete)
            {
                _writer.WriteEndLambdaDelegate();
                _writer.WriteEndConstructor();
                _writer.WriteEndStatement();
            }
            if (Footer != null && !String.IsNullOrEmpty(Footer.Value))
            {
                _writer.WriteLinePragma(
                    context.GenerateLinePragma(Footer.Location, 0, Footer.Value.Length));
                _writer.WriteSnippet(Footer);
                _writer.WriteLinePragma();
            }
            _writer.WriteHelperTrailer();

            context.GeneratedClass.Members.Add(new CodeSnippetTypeMember(_writer.Content));

#endif
            context.TargetWriterName = _oldWriter;

            // TODO: Make this generate the primary generator
            GenerateEndBlockCode(target, context.CodeTreeBuilder, context);
        }

        public override bool Equals(object obj)
        {
            HelperCodeGenerator other = obj as HelperCodeGenerator;
            return other != null &&
                   base.Equals(other) &&
                   HeaderComplete == other.HeaderComplete &&
                   Equals(Signature, other.Signature);
        }

        public override int GetHashCode()
        {
            return HashCodeCombiner.Start()
                .Add(base.GetHashCode())
                .Add(Signature)
                .CombinedHash;
        }

        public override string ToString()
        {
            return "Helper:" + Signature.ToString("F", CultureInfo.CurrentCulture) + ";" + (HeaderComplete ? "C" : "I");
        }

#if NET45
        // No CodeDOM + This code will not be needed once we transition to the CodeTree

        private void AddStatementToHelper(string statement, CodeLinePragma pragma)
        {
            if (pragma != null)
            {
                _writer.WriteLinePragma(pragma);
            }
            _writer.WriteSnippet(statement);
            _writer.InnerWriter.WriteLine(); // CodeDOM normally inserts an extra line so we need to do so here.
            if (pragma != null)
            {
                _writer.WriteLinePragma();
            }
        }
#endif
    }
}
