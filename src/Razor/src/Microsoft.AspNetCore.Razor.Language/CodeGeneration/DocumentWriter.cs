// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public abstract class DocumentWriter
    {
        public static DocumentWriter CreateDefault(CodeTarget codeTarget, RazorCodeGenerationOptions options)
        {
            if (codeTarget == null)
            {
                throw new ArgumentNullException(nameof(codeTarget));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new DefaultDocumentWriter(codeTarget, options);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method was intended to be static, use CreateDefault instead.")]
        public DocumentWriter Create(CodeTarget codeTarget, RazorCodeGenerationOptions options)
        {
            if (codeTarget == null)
            {
                throw new ArgumentNullException(nameof(codeTarget));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new DefaultDocumentWriter(codeTarget, options);
        }

        public abstract RazorCSharpDocument WriteDocument(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);
    }
}
