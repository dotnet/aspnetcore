// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class RazorSyntaxTreePartialParser
    {
        private SyntaxNode _lastChangeOwner;
        private bool _lastResultProvisional;

        public RazorSyntaxTreePartialParser(RazorSyntaxTree syntaxTree)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            OriginalSyntaxTree = syntaxTree;
            ModifiedSyntaxTreeRoot = syntaxTree.Root;
        }

        // Internal for testing
        internal RazorSyntaxTree OriginalSyntaxTree { get; }

        // Internal for testing
        internal SyntaxNode ModifiedSyntaxTreeRoot { get; private set; }

        public PartialParseResultInternal Parse(SourceChange change)
        {
            var result = GetPartialParseResult(change);

            // Remember if this was provisionally accepted for next partial parse.
            _lastResultProvisional = (result & PartialParseResultInternal.Provisional) == PartialParseResultInternal.Provisional;

            return result;
        }

        private PartialParseResultInternal GetPartialParseResult(SourceChange change)
        {
            var result = PartialParseResultInternal.Rejected;

            // Try the last change owner
            if (_lastChangeOwner != null)
            {
                var editHandler = _lastChangeOwner.GetSpanContext()?.EditHandler ?? SpanEditHandler.CreateDefault();
                if (editHandler.OwnsChange(_lastChangeOwner, change))
                {
                    var editResult = editHandler.ApplyChange(_lastChangeOwner, change);
                    result = editResult.Result;
                    if ((editResult.Result & PartialParseResultInternal.Rejected) != PartialParseResultInternal.Rejected)
                    {
                        ReplaceLastChangeOwner(editResult.EditedNode);
                    }
                }

                return result;
            }

            // Locate the span responsible for this change
            _lastChangeOwner = ModifiedSyntaxTreeRoot.LocateOwner(change);

            if (_lastResultProvisional)
            {
                // Last change owner couldn't accept this, so we must do a full reparse
                result = PartialParseResultInternal.Rejected;
            }
            else if (_lastChangeOwner != null)
            {
                var editHandler = _lastChangeOwner.GetSpanContext()?.EditHandler ?? SpanEditHandler.CreateDefault();
                var editResult = editHandler.ApplyChange(_lastChangeOwner, change);
                result = editResult.Result;
                if ((editResult.Result & PartialParseResultInternal.Rejected) != PartialParseResultInternal.Rejected)
                {
                    ReplaceLastChangeOwner(editResult.EditedNode);
                }
            }

            return result;
        }

        private void ReplaceLastChangeOwner(SyntaxNode editedNode)
        {
            ModifiedSyntaxTreeRoot = ModifiedSyntaxTreeRoot.ReplaceNode(_lastChangeOwner, editedNode);
            foreach (var node in ModifiedSyntaxTreeRoot.DescendantNodes())
            {
                if (node.Green == editedNode.Green)
                {
                    _lastChangeOwner = node;
                    break;
                }
            }
        }
    }
}
