// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

// Optimization pass is the best choice for this class. It's not an optimization, but it also doesn't add semantically
// meaningful information.
internal class MetadataAttributePass : IntermediateNodePassBase, IRazorOptimizationPass
{
    private IMetadataIdentifierFeature _identifierFeature;

    protected override void OnInitialized()
    {
        _identifierFeature = Engine.GetFeature<IMetadataIdentifierFeature>();
    }

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (documentNode.Options == null || documentNode.Options.SuppressMetadataAttributes)
        {
            // Metadata attributes are turned off (or options not populated), nothing to do.
            return;
        }

        if (string.Equals(documentNode.DocumentKind, ComponentDocumentClassifierPass.ComponentDocumentKind, StringComparison.Ordinal))
        {
            // Metadata attributes are not used for components.
            return;
        }

        // We need to be able to compute the data we need for the [RazorCompiledItem] attribute - that includes
        // a full type name, and a document kind, and optionally an identifier.
        //
        // If we can't use [RazorCompiledItem] then we don't care about the rest of the attributes.
        var @namespace = documentNode.FindPrimaryNamespace();
        if (@namespace == null || string.IsNullOrEmpty(@namespace.Content))
        {
            // No namespace node or it's incomplete. Skip.
            return;
        }

        var @class = documentNode.FindPrimaryClass();
        if (@class == null || string.IsNullOrEmpty(@class.ClassName))
        {
            // No class node or it's incomplete. Skip.
            return;
        }

        if (documentNode.DocumentKind == null)
        {
            // No document kind. Skip.
            return;
        }

        var identifier = _identifierFeature?.GetIdentifier(codeDocument, codeDocument.Source);
        if (identifier == null)
        {
            // No identifier. Skip
            return;
        }

        // [RazorCompiledItem] is an [assembly: ... ] attribute, so it needs to be applied at the global scope.
        documentNode.Children.Insert(0, new RazorCompiledItemAttributeIntermediateNode()
        {
            TypeName = @namespace.Content + "." + @class.ClassName,
            Kind = documentNode.DocumentKind,
            Identifier = identifier,
        });

        // Now we need to add a [RazorSourceChecksum] for the source and for each import
        // these are class attributes, so we need to find the insertion point to put them
        // right before the class.
        var insert = (int?)null;
        for (var j = 0; j < @namespace.Children.Count; j++)
        {
            if (object.ReferenceEquals(@namespace.Children[j], @class))
            {
                insert = j;
                break;
            }
        }

        if (insert == null)
        {
            // Can't find a place to put the attributes, just bail.
            return;
        }

        if (documentNode.Options.SuppressMetadataSourceChecksumAttributes)
        {
            // Checksum attributes are turned off (or options not populated), nothing to do.
            return;
        }

        // Checksum of the main source
        var checksum = codeDocument.Source.GetChecksum();
        var checksumAlgorithm = codeDocument.Source.GetChecksumAlgorithm();
        if (checksum == null || checksum.Length == 0 || checksumAlgorithm == null)
        {
            // Don't generate anything unless we have all of the required information.
            return;
        }

        @namespace.Children.Insert((int)insert++, new RazorSourceChecksumAttributeIntermediateNode()
        {
            Checksum = checksum,
            ChecksumAlgorithm = checksumAlgorithm,
            Identifier = identifier,
        });

        // Now process the checksums of the imports
        Debug.Assert(_identifierFeature != null);
        for (var i = 0; i < codeDocument.Imports.Count; i++)
        {
            var import = codeDocument.Imports[i];

            checksum = import.GetChecksum();
            checksumAlgorithm = import.GetChecksumAlgorithm();
            identifier = _identifierFeature.GetIdentifier(codeDocument, import);

            if (checksum == null || checksum.Length == 0 || checksumAlgorithm == null || identifier == null)
            {
                // It's ok to skip an import if we don't have all of the required information.
                continue;
            }

            @namespace.Children.Insert((int)insert++, new RazorSourceChecksumAttributeIntermediateNode()
            {
                Checksum = checksum,
                ChecksumAlgorithm = checksumAlgorithm,
                Identifier = identifier,
            });
        }
    }
}
