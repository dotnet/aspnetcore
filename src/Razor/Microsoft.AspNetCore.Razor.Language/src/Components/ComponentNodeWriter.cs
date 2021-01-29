// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    internal abstract class ComponentNodeWriter : IntermediateNodeWriter, ITemplateTargetExtension
    {
        protected abstract void BeginWriteAttribute(CodeRenderingContext context, string key);

        protected abstract void BeginWriteAttribute(CodeRenderingContext context, IntermediateNode expression);

        protected abstract void WriteReferenceCaptureInnards(CodeRenderingContext context, ReferenceCaptureIntermediateNode node, bool shouldTypeCheck);

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
            context.Diagnostics.Add(ComponentDiagnosticFactory.Create_CodeBlockInAttribute(node.Source, content));
            return;
        }

        // Currently the same for design time and runtime
        public override void WriteComponentTypeInferenceMethod(CodeRenderingContext context, ComponentTypeInferenceMethodIntermediateNode node)
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
            //  public static void CreateFoo_0<T1, T2>(RenderTreeBuilder __builder, int seq, int __seq0, T1 __arg0, int __seq1, global::System.Collections.Generic.List<T2> __arg1, int __seq2, string __arg2)
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

            // TODO: Stop assuming this is the full set of generic type parameters. Since we're
            // now also using synthetic args, they might include some additional unrelated generic
            // type parameters. We need to find any unmatched ones, rewrite them to have unique names,
            // and then include them in the list of type params on this method. Alternatively we could
            // impose the simplifying rule that we only cascade generic type arguments that cover a
            // single type parameter, not multiple.
            writer.Write("<");
            writer.Write(string.Join(", ", node.Component.Component.GetTypeParameters().Select(a => a.Name)));
            writer.Write(">");

            writer.Write("(");
            writer.Write("global::");
            writer.Write(ComponentsApi.RenderTreeBuilder.FullTypeName);
            writer.Write(" ");
            writer.Write(ComponentsApi.RenderTreeBuilder.BuilderParameter);
            writer.Write(", ");
            writer.Write("int seq");

            if (node.ReceivesCascadingGenericTypes != null)
            {
                var i = 0;
                foreach (var cascadingGenericType in node.ReceivesCascadingGenericTypes)
                {
                    writer.Write(", ");
                    writer.Write(cascadingGenericType.ValueExpressionType);
                    writer.Write($" syntheticArg{i++}");
                }
            }

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

            // _builder.OpenComponent<TComponent>(42);
            context.CodeWriter.Write(ComponentsApi.RenderTreeBuilder.BuilderParameter);
            context.CodeWriter.Write(".");
            context.CodeWriter.Write(ComponentsApi.RenderTreeBuilder.OpenComponent);
            context.CodeWriter.Write("<");
            context.CodeWriter.Write(node.Component.TypeName);
            context.CodeWriter.Write(">(");
            context.CodeWriter.Write("seq");
            context.CodeWriter.Write(");");
            context.CodeWriter.WriteLine();

            var index = 0;

            // Preserve order of attributes and splat.
            foreach (var child in node.Component.Children)
            {
                if (child is ComponentAttributeIntermediateNode attribute)
                {
                    context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, ComponentsApi.RenderTreeBuilder.AddAttribute);
                    context.CodeWriter.Write(parameters[index].seqName);
                    context.CodeWriter.Write(", ");

                    context.CodeWriter.Write($"\"{attribute.AttributeName}\"");
                    context.CodeWriter.Write(", ");

                    context.CodeWriter.Write(parameters[index].parameterName);
                    context.CodeWriter.WriteEndMethodInvocation();
                    index++;
                }
                else if (child is SplatIntermediateNode)
                {
                    context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, ComponentsApi.RenderTreeBuilder.AddMultipleAttributes);
                    context.CodeWriter.Write(parameters[index].seqName);
                    context.CodeWriter.Write(", ");

                    context.CodeWriter.Write(parameters[index].parameterName);
                    context.CodeWriter.WriteEndMethodInvocation();
                    index++;
                }
            }

            foreach (var childContent in node.Component.ChildContents)
            {
                context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, ComponentsApi.RenderTreeBuilder.AddAttribute);
                context.CodeWriter.Write(parameters[index].seqName);
                context.CodeWriter.Write(", ");

                context.CodeWriter.Write($"\"{childContent.AttributeName}\"");
                context.CodeWriter.Write(", ");

                context.CodeWriter.Write(parameters[index].parameterName);
                context.CodeWriter.WriteEndMethodInvocation();

                index++;
            }

            foreach (var setKey in node.Component.SetKeys)
            {
                context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, ComponentsApi.RenderTreeBuilder.SetKey);
                context.CodeWriter.Write(parameters[index].parameterName);
                context.CodeWriter.WriteEndMethodInvocation();

                index++;
            }

            foreach (var capture in node.Component.Captures)
            {
                context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, capture.IsComponentCapture ? ComponentsApi.RenderTreeBuilder.AddComponentReferenceCapture : ComponentsApi.RenderTreeBuilder.AddElementReferenceCapture);
                context.CodeWriter.Write(parameters[index].seqName);
                context.CodeWriter.Write(", ");

                var cast = capture.IsComponentCapture ? $"({capture.ComponentCaptureTypeName})" : string.Empty;
                context.CodeWriter.Write($"(__value) => {{ {parameters[index].parameterName}({cast}__value); }}");
                context.CodeWriter.WriteEndMethodInvocation();

                index++;
            }

            context.CodeWriter.WriteInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, ComponentsApi.RenderTreeBuilder.CloseComponent);

            writer.WriteLine("}");

            List<(string seqName, string typeName, string parameterName)> GetParameterDeclarations()
            {
                var p = new List<(string seqName, string typeName, string parameterName)>();

                // Preserve order between attributes and splats
                foreach (var child in node.Component.Children)
                {
                    if (child is ComponentAttributeIntermediateNode attribute)
                    {
                        string typeName;
                        if (attribute.GloballyQualifiedTypeName != null)
                        {
                            typeName = attribute.GloballyQualifiedTypeName;
                        }
                        else
                        {
                            typeName = attribute.TypeName;
                            if (attribute.BoundAttribute != null && !attribute.BoundAttribute.IsGenericTypedProperty())
                            {
                                typeName = "global::" + typeName;
                            }
                        }

                        p.Add(($"__seq{p.Count}", typeName, $"__arg{p.Count}"));
                    }
                    else if (child is SplatIntermediateNode splat)
                    {
                        var typeName = ComponentsApi.AddMultipleAttributesTypeFullName;
                        p.Add(($"__seq{p.Count}", typeName, $"__arg{p.Count}"));
                    }
                }

                foreach (var childContent in node.Component.ChildContents)
                {
                    var typeName = childContent.TypeName;
                    if (childContent.BoundAttribute != null && !childContent.BoundAttribute.IsGenericTypedProperty())
                    {
                        typeName = "global::" + typeName;
                    }
                    p.Add(($"__seq{p.Count}", typeName, $"__arg{p.Count}"));
                }

                foreach (var capture in node.Component.SetKeys)
                {
                    p.Add(($"__seq{p.Count}", "object", $"__arg{p.Count}"));
                }

                foreach (var capture in node.Component.Captures)
                {
                    // The capture type name should already contain the global:: prefix.
                    p.Add(($"__seq{p.Count}", capture.TypeName, $"__arg{p.Count}"));
                }

                return p;
            }
        }
    }
}
