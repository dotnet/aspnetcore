// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public abstract class IntermediateNodeWriter
{
    public abstract void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node);

    public abstract void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node);

    public abstract void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node);

    public abstract void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node);

    public abstract void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node);

    public abstract void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node);

    public abstract void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node);

    public abstract void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node);

    public virtual void WriteComponent(CodeRenderingContext context, ComponentIntermediateNode node)
    {
        throw new NotSupportedException("This writer does not support components.");
    }

    public virtual void WriteComponentAttribute(CodeRenderingContext context, ComponentAttributeIntermediateNode node)
    {
        throw new NotSupportedException("This writer does not support components.");
    }

    public virtual void WriteComponentChildContent(CodeRenderingContext context, ComponentChildContentIntermediateNode node)
    {
        throw new NotSupportedException("This writer does not support components.");
    }

    public virtual void WriteComponentTypeArgument(CodeRenderingContext context, ComponentTypeArgumentIntermediateNode node)
    {
        throw new NotSupportedException("This writer does not support components.");
    }

    public virtual void WriteComponentTypeInferenceMethod(CodeRenderingContext context, ComponentTypeInferenceMethodIntermediateNode node)
    {
        throw new NotSupportedException("This writer does not support components.");
    }

    public virtual void WriteMarkupElement(CodeRenderingContext context, MarkupElementIntermediateNode node)
    {
        throw new NotSupportedException("This writer does not support components.");
    }

    public virtual void WriteMarkupBlock(CodeRenderingContext context, MarkupBlockIntermediateNode node)
    {
        throw new NotSupportedException("This writer does not support components.");
    }

    public virtual void WriteReferenceCapture(CodeRenderingContext context, ReferenceCaptureIntermediateNode node)
    {
        throw new NotSupportedException("This writer does not support components.");
    }

    public virtual void WriteSetKey(CodeRenderingContext context, SetKeyIntermediateNode node)
    {
        throw new NotSupportedException("This writer does not support components.");
    }

    public virtual void WriteSplat(CodeRenderingContext context, SplatIntermediateNode node)
    {
        throw new NotSupportedException("This writer does not support components.");
    }

    public abstract void BeginWriterScope(CodeRenderingContext context, string writer);

    public abstract void EndWriterScope(CodeRenderingContext context);
}
