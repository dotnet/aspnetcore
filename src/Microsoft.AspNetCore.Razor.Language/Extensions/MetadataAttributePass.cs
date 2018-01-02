// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    // Optimization pass is the best choice for this class. It's not an optimization, but it also doesn't add semantically
    // meaningful information.
    internal class MetadataAttributePass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            if (documentNode.Options == null || documentNode.Options.SuppressMetadataAttributes)
            {
                // Metadata attributes are turned off (or options not populated), nothing to do.
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

            var identifier = codeDocument.GetIdentifier();
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

            // Checksum of the main source
            AddChecksum(codeDocument.Source.GetChecksum(), codeDocument.Source.GetChecksumAlgorithm(), identifier);

            // Now process the checksums of the imports
            //
            // It's possible that the counts of these won't match, just process as many as we can.
            var importIdentifiers = codeDocument.GetImportIdentifiers() ?? Array.Empty<string>();
            for (var i = 0; i < codeDocument.Imports.Count && i < importIdentifiers.Count; i++)
            {
                var import = codeDocument.Imports[i];
                AddChecksum(import.GetChecksum(), import.GetChecksumAlgorithm(), importIdentifiers[i]);
            }

            void AddChecksum(byte[] checksum, string checksumAlgorithm, string id)
            {
                if (checksum == null || checksum.Length == 0 || checksumAlgorithm == null || id == null)
                {
                    // Don't generate anything unless we have all of the required information.
                    return;
                }

                // Checksum of the main source
                @namespace.Children.Insert((int)insert++, new RazorSourceChecksumAttributeIntermediateNode()
                {
                    Checksum = checksum,
                    ChecksumAlgorithm = checksumAlgorithm,
                    Identifier = id,
                });
            }
        }
    }
}
