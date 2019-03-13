// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    internal class ComponentDocumentClassifierPass : DocumentClassifierPassBase
    {
        public static readonly string ComponentDocumentKind = "component.1.0";
        private static readonly object BuildRenderTreeBaseCallAnnotation = new object();
        private static readonly char[] PathSeparators = new char[] { '/', '\\' };
        private static readonly char[] NamespaceSeparators = new char[] { '.' };

        /// <summary>
        /// The fallback value of the root namespace. Only used if the fallback root namespace
        /// was not passed in.
        /// </summary>
        public string FallbackRootNamespace { get; set; } = "__GeneratedComponent";

        /// <summary>
        /// Gets or sets whether to mangle class names.
        /// 
        /// Set to true in the IDE so we can generated mangled class names. This is needed
        /// to avoid conflicts between generated design-time code and the code in the editor.
        ///
        /// A better workaround for this would be to create a singlefilegenerator that overrides
        /// the codegen process when a document is open, but this is more involved, so hacking
        /// it for now.
        /// </summary>
        public bool MangleClassNames { get; set; } = false;

        protected override string DocumentKind => ComponentDocumentKind;

        // Ensure this runs before the MVC classifiers which have Order = 0
        public override int Order => -100;

        protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            return FileKinds.IsComponent(codeDocument.GetFileKind());
        }

        protected override CodeTarget CreateTarget(RazorCodeDocument codeDocument, RazorCodeGenerationOptions options)
        {
            return new ComponentCodeTarget(options, TargetExtensions);
        }

        /// <inheritdoc />
        protected override void OnDocumentStructureCreated(
            RazorCodeDocument codeDocument,
            NamespaceDeclarationIntermediateNode @namespace,
            ClassDeclarationIntermediateNode @class,
            MethodDeclarationIntermediateNode method)
        {
            var options = codeDocument.GetDocumentIntermediateNode().Options;
            if (!TryComputeNamespaceAndClass(
                options,
                codeDocument.Source.FilePath,
                codeDocument.Source.RelativePath,
                out var computedNamespace,
                out var computedClass))
            {
                // If we can't compute a nice namespace (no relative path) then just generate something
                // mangled.
                computedNamespace = FallbackRootNamespace;
                var checksum = Checksum.BytesToString(codeDocument.Source.GetChecksum());
                computedClass = $"AspNetCore_{checksum}";
            }

            if (MangleClassNames)
            {
                computedClass = "__" + computedClass;
            }

            @namespace.Content = computedNamespace;

            @class.BaseType = ComponentsApi.ComponentBase.FullTypeName;
            @class.ClassName = computedClass;
            @class.Modifiers.Clear();
            @class.Modifiers.Add("public");

            var documentNode = codeDocument.GetDocumentIntermediateNode();
            var typeParamReferences = documentNode.FindDirectiveReferences(ComponentTypeParamDirective.Directive);
            for (var i = 0; i < typeParamReferences.Count; i++)
            {
                var typeParamNode = (DirectiveIntermediateNode)typeParamReferences[i].Node;
                if (typeParamNode.HasDiagnostics)
                {
                    continue;
                }

                @class.TypeParameters.Add(new TypeParameter() { ParameterName = typeParamNode.Tokens.First().Content, });
            }

            method.ReturnType = "void";
            method.MethodName = ComponentsApi.ComponentBase.BuildRenderTree;
            method.Modifiers.Clear();
            method.Modifiers.Add("protected");
            method.Modifiers.Add("override");

            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter()
            {
                ParameterName = "builder",
                TypeName = ComponentsApi.RenderTreeBuilder.FullTypeName,
            });

            // We need to call the 'base' method as the first statement.
            var callBase = new CSharpCodeIntermediateNode();
            callBase.Annotations.Add(BuildRenderTreeBaseCallAnnotation, true);
            callBase.Children.Add(new IntermediateToken
            {
                Kind = TokenKind.CSharp,
                Content = $"base.{ComponentsApi.ComponentBase.BuildRenderTree}(builder);"
            });
            method.Children.Insert(0, callBase);
        }

        // In general documents will have a relative path (relative to the project root).
        // We can only really compute a nice class/namespace when we know a relative path.
        //
        // However all kinds of thing are possible in tools. We shouldn't barf here if the document isn't 
        // set up correctly.
        private bool TryComputeNamespaceAndClass(
            RazorCodeGenerationOptions options, 
            string filePath, 
            string relativePath, 
            out string @namespace, 
            out string @class)
        {
            if (filePath == null || relativePath == null || filePath.Length <= relativePath.Length)
            {
                @namespace = null;
                @class = null;
                return false;
            }

            var rootNamespace = options.RootNamespace;
            if (string.IsNullOrEmpty(rootNamespace))
            {
                @namespace = null;
                @class = null;
                return false;
            }

            var builder = new StringBuilder();

            // Sanitize the base namespace, but leave the dots.
            var segments = rootNamespace.Split(NamespaceSeparators, StringSplitOptions.RemoveEmptyEntries);
            builder.Append(CSharpIdentifier.SanitizeIdentifier(segments[0]));
            for (var i = 1; i < segments.Length; i++)
            {
                builder.Append('.');
                builder.Append(CSharpIdentifier.SanitizeIdentifier(segments[i]));
            }

            segments = relativePath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);

            // Skip the last segment because it's the FileName.
            for (var i = 0; i < segments.Length - 1; i++)
            {
                builder.Append('.');
                builder.Append(CSharpIdentifier.SanitizeIdentifier(segments[i]));
            }

            @namespace = builder.ToString();
            @class = CSharpIdentifier.SanitizeIdentifier(Path.GetFileNameWithoutExtension(relativePath));

            return true;
        }

        internal static bool IsBuildRenderTreeBaseCall(CSharpCodeIntermediateNode node)
            => node.Annotations[BuildRenderTreeBaseCallAnnotation] != null;
    }
}
