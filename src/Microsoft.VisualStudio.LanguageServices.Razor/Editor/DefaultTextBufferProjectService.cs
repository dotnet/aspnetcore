// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    /// <summary>
    /// Infrastructure methods to find project information from an <see cref="ITextBuffer"/>.
    /// </summary>
    [Export(typeof(TextBufferProjectService))]
    internal class DefaultTextBufferProjectService : TextBufferProjectService
    {
        private const string DotNetCoreCapability = "(CSharp|VB)&CPS";

        private readonly RunningDocumentTable _documentTable;
        private readonly ITextDocumentFactoryService _documentFactory;

        [ImportingConstructor]
        public DefaultTextBufferProjectService(
            [Import(typeof(SVsServiceProvider))] IServiceProvider services,
            ITextDocumentFactoryService documentFactory,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (documentFactory == null)
            {
                throw new ArgumentNullException(nameof(documentFactory));
            }

            _documentFactory = documentFactory;
            _documentTable = new RunningDocumentTable(services);
        }

        public override IVsHierarchy GetHierarchy(ITextBuffer textBuffer)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            // If there's no document we can't find the FileName, or look for a matching hierarchy.
            if (!_documentFactory.TryGetTextDocument(textBuffer, out var textDocument))
            {
                return null;
            }

            _documentTable.FindDocument(textDocument.FilePath, out var hierarchy, out uint itemId, out uint cookie);

            // We don't currently try to look a Roslyn ProjectId at this point, we just want to know some
            // basic things.
            // See https://github.com/dotnet/roslyn/blob/4e3db2b7a0732d45a720e9ed00c00cd22ab67a14/src/VisualStudio/Core/SolutionExplorerShim/HierarchyItemToProjectIdMap.cs#L47
            // for a more complete implementation.
            return hierarchy;
        }

        public override string GetProjectPath(IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
            {
                throw new ArgumentNullException(nameof(hierarchy));
            }

            ErrorHandler.ThrowOnFailure(((IVsProject)hierarchy).GetMkDocument((uint)VSConstants.VSITEMID.Root, out var path), VSConstants.E_NOTIMPL);
            return path;
        }

        public override bool IsSupportedProject(IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
            {
                throw new ArgumentNullException(nameof(hierarchy));
            }

            try
            {
                return hierarchy.IsCapabilityMatch(DotNetCoreCapability);
            }
            catch (NotSupportedException)
            {
                // IsCapabilityMatch throws a NotSupportedException if it can't create a BooleanSymbolExpressionEvaluator COM object
            }
            catch (ObjectDisposedException)
            {
                // IsCapabilityMatch throws an ObjectDisposedException if the underlying hierarchy has been disposed.
            }

            return false;
        }
    }
}
