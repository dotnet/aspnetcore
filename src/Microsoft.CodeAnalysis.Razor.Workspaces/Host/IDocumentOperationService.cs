// Copyright (c) Microsoft.  All Rights Reserved.
// Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if DOCUMENT_SERVICE_FACTORY

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// provide various operations for this document
    /// 
    /// I followed name from EditorOperation for now. 
    /// </summary>
    internal interface IDocumentOperationService : IDocumentService
    {
        /// <summary>
        /// document version of <see cref="Workspace.CanApplyChange(ApplyChangesKind)"/>
        /// </summary>
        bool CanApplyChange { get; }

        /// <summary>
        /// indicates whether this document supports diagnostics or not
        /// </summary>
        bool SupportDiagnostics { get; }
    }
}

#endif