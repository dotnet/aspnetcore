// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal abstract class BlazorNodeWriter : IntermediateNodeWriter
    {
        public sealed override void BeginWriterScope(CodeRenderingContext context, string writer)
        {
            throw new NotImplementedException(nameof(BeginWriterScope));
        }

        public sealed override void EndWriterScope(CodeRenderingContext context)
        {
            throw new NotImplementedException(nameof(EndWriterScope));
        }

        public abstract void BeginWriteAttribute(CodeWriter codeWriter, string key);

        public abstract void WriteComponentOpen(CodeRenderingContext context, ComponentOpenExtensionNode node);

        public abstract void WriteComponentClose(CodeRenderingContext context, ComponentCloseExtensionNode node);

        public abstract void WriteComponentBody(CodeRenderingContext context, ComponentBodyExtensionNode node);

        public abstract void WriteComponentAttribute(CodeRenderingContext context, ComponentAttributeExtensionNode node);

        public abstract void WriteReferenceCapture(CodeRenderingContext context, RefExtensionNode node);
    }
}
