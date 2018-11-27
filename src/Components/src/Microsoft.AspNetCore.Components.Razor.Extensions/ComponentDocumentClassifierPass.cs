// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Components.Shared;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    /// <summary>
    /// A <see cref="DocumentClassifierPassBase"/> that recognizes Blazor components.
    /// </summary>
    public class ComponentDocumentClassifierPass : DocumentClassifierPassBase, IRazorDocumentClassifierPass
    {
        /// <summary>
        /// The component document kind.
        /// </summary>
        public static readonly string ComponentDocumentKind = "Blazor.Component";

        private static readonly object BuildRenderTreeBaseCallAnnotation = new object();

        private static readonly char[] PathSeparators = new char[] { '/', '\\' };

        private static readonly char[] NamespaceSeparators = new char[] { '.' };

        /// <summary>
        /// The base namespace.
        /// </summary>
        // This is a fallback value and will only be used if we can't compute
        // a reasonable namespace.
        public string BaseNamespace { get; set; } = "__BlazorGenerated";

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

        internal static bool IsBuildRenderTreeBaseCall(CSharpCodeIntermediateNode node)
            => node.Annotations[BuildRenderTreeBaseCallAnnotation] != null;

        /// <inheritdoc />
        protected override string DocumentKind => ComponentDocumentKind;

        /// <inheritdoc />
        protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            // Treat everything as a component by default if Blazor is part of the configuration.
            return true;
        }

        /// <inheritdoc />
        protected override void OnDocumentStructureCreated(
            RazorCodeDocument codeDocument, 
            NamespaceDeclarationIntermediateNode @namespace, 
            ClassDeclarationIntermediateNode @class, 
            MethodDeclarationIntermediateNode method)
        {
            if (!TryComputeNamespaceAndClass(
                codeDocument.Source.FilePath,
                codeDocument.Source.RelativePath, 
                out var computedNamespace,
                out var computedClass))
            {
                // If we can't compute a nice namespace (no relative path) then just generate something
                // mangled.
                computedNamespace = BaseNamespace;
                computedClass = CSharpIdentifier.GetClassNameFromPath(codeDocument.Source.FilePath) ?? "__BlazorComponent";
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
            var typeParamReferences = documentNode.FindDirectiveReferences(TypeParamDirective.Directive);
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
        private bool TryComputeNamespaceAndClass(string filePath, string relativePath, out string @namespace, out string @class)
        {
            if (filePath == null || relativePath == null || filePath.Length <= relativePath.Length)
            {
                @namespace = null;
                @class = null;
                return false;
            }

            // Try and infer a namespace from the project directory. We don't yet have the ability to pass
            // the namespace through from the project.
            var trimLength = relativePath.Length + (relativePath.StartsWith("/") ? 0 : 1);
            var baseDirectory = filePath.Substring(0, filePath.Length - trimLength);

            var lastSlash = baseDirectory.LastIndexOfAny(PathSeparators);
            var baseNamespace = lastSlash == -1 ? baseDirectory : baseDirectory.Substring(lastSlash + 1);
            if (string.IsNullOrEmpty(baseNamespace))
            {
                @namespace = null;
                @class = null;
                return false;
            }

            var builder = new StringBuilder();

            // Sanitize the base namespace, but leave the dots.
            var segments = baseNamespace.Split(NamespaceSeparators, StringSplitOptions.RemoveEmptyEntries);
            builder.Append(CSharpIdentifier.SanitizeClassName(segments[0]));
            for (var i = 1; i < segments.Length; i++)
            {
                builder.Append('.');
                builder.Append(CSharpIdentifier.SanitizeClassName(segments[i]));
            }

            segments = relativePath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);

            // Skip the last segment because it's the FileName.
            for (var i = 0; i < segments.Length - 1; i++)
            {
                builder.Append('.');
                builder.Append(CSharpIdentifier.SanitizeClassName(segments[i]));
            }

            @namespace = builder.ToString();
            @class = CSharpIdentifier.SanitizeClassName(Path.GetFileNameWithoutExtension(relativePath));

            return true;
        }

        #region Workaround
        // This is a workaround for the fact that the base class doesn't provide good support
        // for replacing the IntermediateNodeWriter when building the code target. 
        void IRazorDocumentClassifierPass.Execute(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            base.Execute(codeDocument, documentNode);
            documentNode.Target = new BlazorCodeTarget(documentNode.Options, _targetExtensions);
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            var feature = Engine.Features.OfType<IRazorTargetExtensionFeature>();
            _targetExtensions = feature.FirstOrDefault()?.TargetExtensions.ToArray() ?? EmptyExtensionArray;
        }

        private static readonly ICodeTargetExtension[] EmptyExtensionArray = new ICodeTargetExtension[0];
        private ICodeTargetExtension[] _targetExtensions;
        #endregion
    }
}
