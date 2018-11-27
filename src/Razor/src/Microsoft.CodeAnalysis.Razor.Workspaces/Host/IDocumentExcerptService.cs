// Copyright (c) Microsoft.  All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DOCUMENT_SERVICE_FACTORY

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// excerpt some part of <see cref="Document"/>
    /// </summary>
    internal interface IDocumentExcerptService : IDocumentService
    {
        /// <summary>
        /// return <see cref="ExcerptResult"/> of given <see cref="Document"/> and <see cref="TextSpan"/>
        /// 
        /// the result might not be an exact copy of the given source or contains more then given span
        /// </summary>
        Task<ExcerptResult?> TryExcerptAsync(Document document, TextSpan span, ExcerptMode mode, CancellationToken cancellationToken);
    }

    /// <summary>
    /// this mode shows intention not actual behavior. it is up to implementation how to interpret the intention.
    /// </summary>
    internal enum ExcerptMode
    {
        SingleLine,
        Tooltip
    }

    /// <summary>
    /// Result of excerpt
    /// </summary>
    internal struct ExcerptResult
    {
        /// <summary>
        /// excerpt content
        /// </summary>
        public readonly SourceText Content;

        /// <summary>
        /// span on <see cref="Content"/> that given <see cref="Span"/> got mapped to
        /// </summary>
        public readonly TextSpan MappedSpan;

        /// <summary>
        /// classification information on the <see cref="Content"/>
        /// </summary>
        public readonly ImmutableArray<ClassifiedSpan> ClassifiedSpans;

        /// <summary>
        /// <see cref="Document"/> this excerpt is from
        /// </summary>
        public readonly Document Document;

        /// <summary>
        /// span on <see cref="Document"/> this excerpt is from
        /// </summary>
        public readonly TextSpan Span;

        public ExcerptResult(SourceText content, TextSpan mappedSpan, ImmutableArray<ClassifiedSpan> classifiedSpans, Document document, TextSpan span)
        {
            Content = content;
            MappedSpan = mappedSpan;
            ClassifiedSpans = classifiedSpans;

            // these 2 might not actually needed
            Document = document;
            Span = span;
        }
    }
}

#endif