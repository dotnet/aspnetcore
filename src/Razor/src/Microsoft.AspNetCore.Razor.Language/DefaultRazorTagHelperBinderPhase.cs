// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorTagHelperBinderPhase : RazorEnginePhaseBase, IRazorTagHelperBinderPhase
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var syntaxTree = codeDocument.GetSyntaxTree();
            ThrowForMissingDocumentDependency(syntaxTree);

            var descriptors = codeDocument.GetTagHelpers();
            if (descriptors == null)
            {
                var feature = Engine.Features.OfType<ITagHelperFeature>().FirstOrDefault();
                if (feature == null)
                {
                    // No feature, nothing to do.
                    return;
                }

                descriptors = feature.GetDescriptors();
            }

            // We need to find directives in all of the *imports* as well as in the main razor file
            //
            // The imports come logically before the main razor file and are in the order they
            // should be processed.
            var visitor = new DirectiveVisitor(descriptors);
            var imports = codeDocument.GetImportSyntaxTrees();
            if (imports != null)
            {
                for (var i = 0; i < imports.Count; i++)
                {
                    var import = imports[i];
                    visitor.Visit(import.Root);
                }
            }

            visitor.Visit(syntaxTree.Root);

            var tagHelperPrefix = visitor.TagHelperPrefix;
            descriptors = visitor.Matches.ToArray();

            var context = TagHelperDocumentContext.Create(tagHelperPrefix, descriptors);
            codeDocument.SetTagHelperContext(context);

            if (descriptors.Count == 0)
            {
                // No descriptors, no-op.
                return;
            }

            var rewrittenSyntaxTree = TagHelperParseTreeRewriter.Rewrite(syntaxTree, tagHelperPrefix, descriptors);
            
            codeDocument.SetSyntaxTree(rewrittenSyntaxTree);
        }

        private static bool MatchesDirective(TagHelperDescriptor descriptor, string typePattern, string assemblyName)
        {
            if (!string.Equals(descriptor.AssemblyName, assemblyName, StringComparison.Ordinal))
            {
                return false;
            }

            if (typePattern.EndsWith("*", StringComparison.Ordinal))
            {
                if (typePattern.Length == 1)
                {
                    // TypePattern is "*".
                    return true;
                }

                var lookupTypeName = typePattern.Substring(0, typePattern.Length - 1);

                return descriptor.Name.StartsWith(lookupTypeName, StringComparison.Ordinal);
            }

            return string.Equals(descriptor.Name, typePattern, StringComparison.Ordinal);
        }

        internal class DirectiveVisitor : SyntaxRewriter
        {
            private IReadOnlyList<TagHelperDescriptor> _tagHelpers;

            public DirectiveVisitor(IReadOnlyList<TagHelperDescriptor> tagHelpers)
            {
                _tagHelpers = tagHelpers;
            }

            public string TagHelperPrefix { get; private set; }

            public HashSet<TagHelperDescriptor> Matches { get; } = new HashSet<TagHelperDescriptor>();

            public override SyntaxNode VisitRazorDirective(RazorDirectiveSyntax node)
            {
                var descendantLiterals = node.DescendantNodes();
                foreach (var child in descendantLiterals)
                {
                    if (!(child is CSharpStatementLiteralSyntax literal))
                    {
                        continue;
                    }

                    var context = literal.GetSpanContext();
                    if (context == null)
                    {
                        // We can't find a chunk generator.
                        continue;
                    }
                    else if (context.ChunkGenerator is AddTagHelperChunkGenerator addTagHelper)
                    {
                        if (addTagHelper.AssemblyName == null)
                        {
                            // Skip this one, it's an error
                            continue;
                        }

                        if (!AssemblyContainsTagHelpers(addTagHelper.AssemblyName, _tagHelpers))
                        {
                            // No tag helpers in the assembly.
                            continue;
                        }

                        for (var i = 0; i < _tagHelpers.Count; i++)
                        {
                            var tagHelper = _tagHelpers[i];
                            if (MatchesDirective(tagHelper, addTagHelper.TypePattern, addTagHelper.AssemblyName))
                            {
                                Matches.Add(tagHelper);
                            }
                        }
                    }
                    else if (context.ChunkGenerator is RemoveTagHelperChunkGenerator removeTagHelper)
                    {
                        if (removeTagHelper.AssemblyName == null)
                        {
                            // Skip this one, it's an error
                            continue;
                        }


                        if (!AssemblyContainsTagHelpers(removeTagHelper.AssemblyName, _tagHelpers))
                        {
                            // No tag helpers in the assembly.
                            continue;
                        }

                        for (var i = 0; i < _tagHelpers.Count; i++)
                        {
                            var tagHelper = _tagHelpers[i];
                            if (MatchesDirective(tagHelper, removeTagHelper.TypePattern, removeTagHelper.AssemblyName))
                            {
                                Matches.Remove(tagHelper);
                            }
                        }
                    }
                    else if (context.ChunkGenerator is TagHelperPrefixDirectiveChunkGenerator tagHelperPrefix)
                    {
                        if (!string.IsNullOrEmpty(tagHelperPrefix.DirectiveText))
                        {
                            // We only expect to see a single one of these per file, but that's enforced at another level.
                            TagHelperPrefix = tagHelperPrefix.DirectiveText;
                        }
                    }
                }

                return base.VisitRazorDirective(node);
            }

            private bool AssemblyContainsTagHelpers(string assemblyName, IReadOnlyList<TagHelperDescriptor> tagHelpers)
            {
                for (var i = 0; i < tagHelpers.Count; i++)
                {
                    if (string.Equals(tagHelpers[i].AssemblyName, assemblyName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
