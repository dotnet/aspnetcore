// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Shared;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal abstract class BlazorNodeWriter : IntermediateNodeWriter
    {
        public abstract void BeginWriteAttribute(CodeWriter codeWriter, string key);

        public abstract void WriteComponent(CodeRenderingContext context, ComponentExtensionNode node);

        public abstract void WriteComponentAttribute(CodeRenderingContext context, ComponentAttributeExtensionNode node);

        public abstract void WriteComponentChildContent(CodeRenderingContext context, ComponentChildContentIntermediateNode node);

        public abstract void WriteComponentTypeArgument(CodeRenderingContext context, ComponentTypeArgumentExtensionNode node);

        public abstract void WriteHtmlElement(CodeRenderingContext context, HtmlElementIntermediateNode node);

        public abstract void WriteHtmlBlock(CodeRenderingContext context, HtmlBlockIntermediateNode node);

        public abstract void WriteReferenceCapture(CodeRenderingContext context, RefExtensionNode node);

        protected abstract void WriteReferenceCaptureInnards(CodeRenderingContext context, RefExtensionNode node, bool shouldTypeCheck);

        public abstract void WriteTemplate(CodeRenderingContext context, TemplateIntermediateNode node);

        public sealed override void BeginWriterScope(CodeRenderingContext context, string writer)
        {
            throw new NotImplementedException(nameof(BeginWriterScope));
        }

        public sealed override void EndWriterScope(CodeRenderingContext context)
        {
            throw new NotImplementedException(nameof(EndWriterScope));
        }

        public sealed override void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node)
        {
            // We used to support syntaxes like <elem onsomeevent=@{ /* some C# code */ } /> but this is no longer the 
            // case.
            //
            // We provide an error for this case just to be friendly.
            var content = string.Join("", node.Children.OfType<IntermediateToken>().Select(t => t.Content));
            context.Diagnostics.Add(BlazorDiagnosticFactory.Create_CodeBlockInAttribute(node.Source, content));
            return;
        }


        // Currently the same for design time and runtime
        public void WriteComponentTypeInferenceMethod(CodeRenderingContext context, ComponentTypeInferenceMethodIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // This is ugly because CodeWriter doesn't allow us to erase, but we need to comma-delimit. So we have to
            // materizalize something can iterate, or use string.Join. We'll need this multiple times, so materializing
            // it.
            var parameters = GetParameterDeclarations();

            // This is really similar to the code in WriteComponentAttribute and WriteComponentChildContent - except simpler because
            // attributes and child contents look like variables. 
            //
            // Looks like:
            //
            //  public static void CreateFoo_0<T1, T2>(RenderTreeBuilder builder, int seq, int __seq0, T1 __arg0, int __seq1, global::System.Collections.Generic.List<T2> __arg1, int __seq2, string __arg2)
            //  {
            //      builder.OpenComponent<Foo<T1, T2>>();
            //      builder.AddAttribute(__seq0, "Attr0", __arg0);
            //      builder.AddAttribute(__seq1, "Attr1", __arg1);
            //      builder.AddAttribute(__seq2, "Attr2", __arg2);
            //      builder.CloseComponent();
            //  }
            //
            // As a special case, we need to generate a thunk for captures in this block instead of using
            // them verbatim.
            //
            // The problem is that RenderTreeBuilder wants an Action<object>. The caller can't write the type
            // name if it contains generics, and we can't write the variable they want to assign to. 
            var writer = context.CodeWriter;

            writer.Write("public static void ");
            writer.Write(node.MethodName);

            writer.Write("<");
            writer.Write(string.Join(", ", node.Component.Component.GetTypeParameters().Select(a => a.Name)));
            writer.Write(">");

            writer.Write("(");
            writer.Write("global::");
            writer.Write(ComponentsApi.RenderTreeBuilder.FullTypeName);
            writer.Write(" builder");
            writer.Write(", ");
            writer.Write("int seq");

            if (parameters.Count > 0)
            {
                writer.Write(", ");
            }

            for (var i = 0; i < parameters.Count; i++)
            {
                writer.Write("int ");
                writer.Write(parameters[i].seqName);

                writer.Write(", ");
                writer.Write(parameters[i].typeName);
                writer.Write(" ");
                writer.Write(parameters[i].parameterName);

                if (i < parameters.Count - 1)
                {
                    writer.Write(", ");
                }
            }

            writer.Write(")");
            writer.WriteLine();

            writer.WriteLine("{");

            // builder.OpenComponent<TComponent>(42);
            context.CodeWriter.Write("builder");
            context.CodeWriter.Write(".");
            context.CodeWriter.Write(ComponentsApi.RenderTreeBuilder.OpenComponent);
            context.CodeWriter.Write("<");
            context.CodeWriter.Write(node.Component.TypeName);
            context.CodeWriter.Write(">(");
            context.CodeWriter.Write("seq");
            context.CodeWriter.Write(");");
            context.CodeWriter.WriteLine();

            var index = 0;
            foreach (var attribute in node.Component.Attributes)
            {
                context.CodeWriter.WriteStartInstanceMethodInvocation("builder", ComponentsApi.RenderTreeBuilder.AddAttribute);
                context.CodeWriter.Write(parameters[index].seqName);
                context.CodeWriter.Write(", ");

                context.CodeWriter.Write($"\"{attribute.AttributeName}\"");
                context.CodeWriter.Write(", ");

                context.CodeWriter.Write(parameters[index].parameterName);
                context.CodeWriter.WriteEndMethodInvocation();

                index++;
            }

            foreach (var childContent in node.Component.ChildContents)
            {
                context.CodeWriter.WriteStartInstanceMethodInvocation("builder", ComponentsApi.RenderTreeBuilder.AddAttribute);
                context.CodeWriter.Write(parameters[index].seqName);
                context.CodeWriter.Write(", ");

                context.CodeWriter.Write($"\"{childContent.AttributeName}\"");
                context.CodeWriter.Write(", ");

                context.CodeWriter.Write(parameters[index].parameterName);
                context.CodeWriter.WriteEndMethodInvocation();

                index++;
            }

            foreach (var capture in node.Component.Captures)
            {
                context.CodeWriter.WriteStartInstanceMethodInvocation("builder", capture.IsComponentCapture ? ComponentsApi.RenderTreeBuilder.AddComponentReferenceCapture : ComponentsApi.RenderTreeBuilder.AddElementReferenceCapture);
                context.CodeWriter.Write(parameters[index].seqName);
                context.CodeWriter.Write(", ");

                var cast = capture.IsComponentCapture ? $"({capture.ComponentCaptureTypeName})" : string.Empty;
                context.CodeWriter.Write($"(__value) => {{ {parameters[index].parameterName}({cast}__value); }}");
                context.CodeWriter.WriteEndMethodInvocation();

                index++;
            }

            context.CodeWriter.WriteInstanceMethodInvocation("builder", ComponentsApi.RenderTreeBuilder.CloseComponent);

            writer.WriteLine("}");

            List<(string seqName, string typeName, string parameterName)> GetParameterDeclarations()
            {
                var p = new List<(string seqName, string typeName, string parameterName)>();
                foreach (var attribute in node.Component.Attributes)
                {
                    p.Add(($"__seq{p.Count}", attribute.TypeName, $"__arg{p.Count}"));
                }

                foreach (var childContent in node.Component.ChildContents)
                {
                    p.Add(($"__seq{p.Count}", childContent.TypeName, $"__arg{p.Count}"));
                }

                foreach (var capture in node.Component.Captures)
                {
                    p.Add(($"__seq{p.Count}", capture.TypeName, $"__arg{p.Count}"));
                }

                return p;
            }
        }
    }
}
