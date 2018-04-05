// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class DefaultDocumentWriter : DocumentWriter
    {
        private readonly CodeTarget _codeTarget;
        private readonly RazorCodeGenerationOptions _options;

        public DefaultDocumentWriter(CodeTarget codeTarget, RazorCodeGenerationOptions options)
        {
            _codeTarget = codeTarget;
            _options = options;
        }

        public override RazorCSharpDocument WriteDocument(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (documentNode == null)
            {
                throw new ArgumentNullException(nameof(documentNode));
            }

            var context = new DefaultCodeRenderingContext(
                new CodeWriter(),
                _codeTarget.CreateNodeWriter(),
                codeDocument,
                documentNode,
                _options);
            context.Visitor = new Visitor(_codeTarget, context);

            context.Visitor.VisitDocument(documentNode);

            var cSharp = context.CodeWriter.GenerateCode();
            return new DefaultRazorCSharpDocument(
                cSharp,
                _options,
                context.Diagnostics.ToArray(),
                context.SourceMappings.ToArray());
        }

        private class Visitor : IntermediateNodeVisitor
        {
            private readonly DefaultCodeRenderingContext _context;
            private readonly CodeTarget _target;

            public Visitor(CodeTarget target, DefaultCodeRenderingContext context)
            {
                _target = target;
                _context = context;
            }

            private DefaultCodeRenderingContext Context => _context;

            public override void VisitDocument(DocumentIntermediateNode node)
            {
                if (!Context.Options.SuppressChecksum)
                {
                    // See http://msdn.microsoft.com/en-us/library/system.codedom.codechecksumpragma.checksumalgorithmid.aspx
                    // And https://github.com/dotnet/roslyn/blob/614299ff83da9959fa07131c6d0ffbc58873b6ae/src/Compilers/Core/Portable/PEWriter/DebugSourceDocument.cs#L67
                    //
                    // We only support algorithms that the debugger understands, which is currently SHA1 and SHA256.

                    string algorithmId;
                    var algorithm = Context.SourceDocument.GetChecksumAlgorithm();
                    if (string.Equals(algorithm, HashAlgorithmName.SHA256.Name, StringComparison.Ordinal))
                    {
                        algorithmId = "{8829d00f-11b8-4213-878b-770e8597ac16}";
                    }
                    else if (string.Equals(algorithm, HashAlgorithmName.SHA1.Name, StringComparison.Ordinal) ||

                        // In 2.0, we didn't actually expose the name of the algorithm, so it's possible we could get null here.
                        // If that's the case, we just assume SHA1 since that's the only thing we supported in 2.0.
                        algorithm == null)
                    {
                        algorithmId = "{ff1816ec-aa5e-4d10-87f7-6f4963833460}";
                    }
                    else
                    {
                        var supportedAlgorithms = string.Join(" ", new string[]
                        {
                            HashAlgorithmName.SHA1.Name,
                            HashAlgorithmName.SHA256.Name
                        });

                        var message = Resources.FormatUnsupportedChecksumAlgorithm(
                            algorithm,
                            supportedAlgorithms,
                            nameof(RazorCodeGenerationOptions) + "." + nameof(RazorCodeGenerationOptions.SuppressChecksum),
                            bool.TrueString);
                        throw new InvalidOperationException(message);
                    }

                    var sourceDocument = Context.SourceDocument;

                    var checksum = Checksum.BytesToString(sourceDocument.GetChecksum());
                    if (!string.IsNullOrEmpty(checksum))
                    {
                        Context.CodeWriter
                            .Write("#pragma checksum \"")
                            .Write(sourceDocument.FilePath)
                            .Write("\" \"")
                            .Write(algorithmId)
                            .Write("\" \"")
                            .Write(checksum)
                            .WriteLine("\"");
                    }
                }

                Context.CodeWriter
                    .WriteLine("// <auto-generated/>")
                    .WriteLine("#pragma warning disable 1591");

                VisitDefault(node);

                Context.CodeWriter.WriteLine("#pragma warning restore 1591");
            }

            public override void VisitUsingDirective(UsingDirectiveIntermediateNode node)
            {
                Context.NodeWriter.WriteUsingDirective(Context, node);
            }

            public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
            {
                using (Context.CodeWriter.BuildNamespace(node.Content))
                {
                    Context.CodeWriter.WriteLine("#line hidden");
                    VisitDefault(node);
                }
            }

            public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
            {
                using (Context.CodeWriter.BuildClassDeclaration(
                    node.Modifiers,
                    node.ClassName,
                    node.BaseType,
                    node.Interfaces,
                    node.TypeParameters.Select(p => p.ParameterName).ToArray()))
                {
                    VisitDefault(node);
                }
            }

            public override void VisitMethodDeclaration(MethodDeclarationIntermediateNode node)
            {
                Context.CodeWriter.WriteLine("#pragma warning disable 1998");

                for (var i = 0; i < node.Modifiers.Count; i++)
                {
                    Context.CodeWriter.Write(node.Modifiers[i]);
                    Context.CodeWriter.Write(" ");
                }

                Context.CodeWriter.Write(node.ReturnType);
                Context.CodeWriter.Write(" ");

                Context.CodeWriter.Write(node.MethodName);
                Context.CodeWriter.Write("(");

                for (var i = 0; i < node.Parameters.Count; i++)
                {
                    var parameter = node.Parameters[i];

                    for (var j = 0; j < parameter.Modifiers.Count; j++)
                    {
                        Context.CodeWriter.Write(parameter.Modifiers[j]);
                        Context.CodeWriter.Write(" ");
                    }

                    Context.CodeWriter.Write(parameter.TypeName);
                    Context.CodeWriter.Write(" ");

                    Context.CodeWriter.Write(parameter.ParameterName);

                    if (i < node.Parameters.Count - 1)
                    {
                        Context.CodeWriter.Write(", ");
                    }
                }

                Context.CodeWriter.Write(")");
                Context.CodeWriter.WriteLine();

                using (Context.CodeWriter.BuildScope())
                {
                    VisitDefault(node);
                }

                Context.CodeWriter.WriteLine("#pragma warning restore 1998");
            }

            public override void VisitFieldDeclaration(FieldDeclarationIntermediateNode node)
            {
                Context.CodeWriter.WriteField(node.Modifiers, node.FieldType, node.FieldName);
            }

            public override void VisitPropertyDeclaration(PropertyDeclarationIntermediateNode node)
            {
                Context.CodeWriter.WriteAutoPropertyDeclaration(node.Modifiers, node.PropertyType, node.PropertyName);
            }

            public override void VisitExtension(ExtensionIntermediateNode node)
            {
                node.WriteNode(_target, Context);
            }

            public override void VisitCSharpExpression(CSharpExpressionIntermediateNode node)
            {
                Context.NodeWriter.WriteCSharpExpression(Context, node);
            }

            public override void VisitCSharpCode(CSharpCodeIntermediateNode node)
            {
                Context.NodeWriter.WriteCSharpCode(Context, node);
            }

            public override void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
            {
                Context.NodeWriter.WriteHtmlAttribute(Context, node);
            }

            public override void VisitHtmlAttributeValue(HtmlAttributeValueIntermediateNode node)
            {
                Context.NodeWriter.WriteHtmlAttributeValue(Context, node);
            }

            public override void VisitCSharpExpressionAttributeValue(CSharpExpressionAttributeValueIntermediateNode node)
            {
                Context.NodeWriter.WriteCSharpExpressionAttributeValue(Context, node);
            }

            public override void VisitCSharpCodeAttributeValue(CSharpCodeAttributeValueIntermediateNode node)
            {
                Context.NodeWriter.WriteCSharpCodeAttributeValue(Context, node);
            }

            public override void VisitHtml(HtmlContentIntermediateNode node)
            {
                Context.NodeWriter.WriteHtmlContent(Context, node);
            }

            public override void VisitTagHelper(TagHelperIntermediateNode node)
            {
                VisitDefault(node);
            }

            public override void VisitDefault(IntermediateNode node)
            {
                Context.RenderChildren(node);
            }
        }
    }
}
