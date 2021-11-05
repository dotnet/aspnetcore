// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentDocumentClassifierPass : DocumentClassifierPassBase
{
    public const string ComponentDocumentKind = "component.1.0";

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
    public bool MangleClassNames { get; set; }

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
        if (!codeDocument.TryComputeNamespace(fallbackToRootNamespace: true, out var computedNamespace) ||
            !TryComputeClassName(codeDocument, out var computedClass))
        {
            // If we can't compute a nice namespace (no relative path) then just generate something
            // mangled.
            computedNamespace = FallbackRootNamespace;
            var checksum = Checksum.BytesToString(codeDocument.Source.GetChecksum());
            computedClass = $"AspNetCore_{checksum}";
        }

        var documentNode = codeDocument.GetDocumentIntermediateNode();
        if (char.IsLower(computedClass, 0))
        {
            // We don't allow component names to start with a lowercase character.
            documentNode.Diagnostics.Add(
                ComponentDiagnosticFactory.Create_ComponentNamesCannotStartWithLowerCase(computedClass, documentNode.Source));
        }

        if (MangleClassNames)
        {
            computedClass = ComponentMetadata.MangleClassName(computedClass);
        }

        @namespace.Content = computedNamespace;
        @class.ClassName = computedClass;
        @class.Modifiers.Clear();
        @class.Modifiers.Add("public");
        @class.Modifiers.Add("partial");

        if (FileKinds.IsComponentImport(codeDocument.GetFileKind()))
        {
            // We don't want component imports to be considered as real component.
            // But we still want to generate code for it so we can get diagnostics.
            @class.BaseType = typeof(object).FullName;

            method.ReturnType = "void";
            method.MethodName = "Execute";
            method.Modifiers.Clear();
            method.Modifiers.Add("protected");

            method.Parameters.Clear();
        }
        else
        {
            @class.BaseType = ComponentsApi.ComponentBase.FullTypeName;

            // Constrained type parameters are only supported in Razor language versions v6.0
            var razorLanguageVersion = codeDocument.GetParserOptions()?.Version ?? RazorLanguageVersion.Latest;
            var directiveType = razorLanguageVersion.CompareTo(RazorLanguageVersion.Version_6_0) >= 0
                ? ComponentConstrainedTypeParamDirective.Directive
                : ComponentTypeParamDirective.Directive;
            var typeParamReferences = documentNode.FindDirectiveReferences(directiveType);
            for (var i = 0; i < typeParamReferences.Count; i++)
            {
                var typeParamNode = (DirectiveIntermediateNode)typeParamReferences[i].Node;
                if (typeParamNode.HasDiagnostics)
                {
                    continue;
                }

                @class.TypeParameters.Add(new TypeParameter()
                {
                    ParameterName = typeParamNode.Tokens.First().Content,
                    Constraints = typeParamNode.Tokens.Skip(1).FirstOrDefault()?.Content
                });
            }

            method.ReturnType = "void";
            method.MethodName = ComponentsApi.ComponentBase.BuildRenderTree;
            method.Modifiers.Clear();
            method.Modifiers.Add("protected");
            method.Modifiers.Add("override");

            method.Parameters.Clear();
            method.Parameters.Add(new MethodParameter()
            {
                ParameterName = ComponentsApi.RenderTreeBuilder.BuilderParameter,
                TypeName = ComponentsApi.RenderTreeBuilder.FullTypeName,
            });
        }
    }

    private bool TryComputeClassName(RazorCodeDocument codeDocument, out string className)
    {
        className = null;
        if (codeDocument.Source.FilePath == null || codeDocument.Source.RelativePath == null)
        {
            return false;
        }

        var relativePath = NormalizePath(codeDocument.Source.RelativePath);
        className = CSharpIdentifier.SanitizeIdentifier(Path.GetFileNameWithoutExtension(relativePath));
        return true;
    }

    private static string NormalizePath(string path)
    {
        path = path.Replace('\\', '/');

        return path;
    }
}
