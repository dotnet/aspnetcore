// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

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

        var parameters = GetTypeInferenceMethodParameters(node);

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

        if (parameters.Count > 0)
        {
            writer.Write(", ");
        }

        for (var i = 0; i < parameters.Count; i++)
        {
            if (!string.IsNullOrEmpty(parameters[i].SeqName))
            {
                writer.Write("int ");
                writer.Write(parameters[i].SeqName);
                writer.Write(", ");
            }

            writer.Write(parameters[i].TypeName);
            writer.Write(" ");
            writer.Write(parameters[i].ParameterName);

            if (i < parameters.Count - 1)
            {
                writer.Write(", ");
            }
        }

        writer.Write(")");

        // Writes out a list of generic type constraints with indentation
        // public void Foo<T, U>(T t, U u)
        //      where T: new()
        //      where U: Foo, notnull
        foreach (var constraint in node.GenericTypeConstraints)
        {
            writer.WriteLine();
            writer.Indent(writer.CurrentIndent + writer.TabSize);
            writer.Write(constraint);

        }

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

        foreach (var parameter in parameters)
        {
            switch (parameter.Source)
            {
                case ComponentAttributeIntermediateNode attribute:
                    context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, ComponentsApi.RenderTreeBuilder.AddAttribute);
                    context.CodeWriter.Write(parameter.SeqName);
                    context.CodeWriter.Write(", ");

                    context.CodeWriter.Write($"\"{attribute.AttributeName}\"");
                    context.CodeWriter.Write(", ");

                    context.CodeWriter.Write(parameter.ParameterName);
                    context.CodeWriter.WriteEndMethodInvocation();
                    break;

                case SplatIntermediateNode:
                    context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, ComponentsApi.RenderTreeBuilder.AddMultipleAttributes);
                    context.CodeWriter.Write(parameter.SeqName);
                    context.CodeWriter.Write(", ");

                    context.CodeWriter.Write(parameter.ParameterName);
                    context.CodeWriter.WriteEndMethodInvocation();
                    break;

                case ComponentChildContentIntermediateNode childContent:
                    context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, ComponentsApi.RenderTreeBuilder.AddAttribute);
                    context.CodeWriter.Write(parameter.SeqName);
                    context.CodeWriter.Write(", ");

                    context.CodeWriter.Write($"\"{childContent.AttributeName}\"");
                    context.CodeWriter.Write(", ");

                    context.CodeWriter.Write(parameter.ParameterName);
                    context.CodeWriter.WriteEndMethodInvocation();
                    break;

                case SetKeyIntermediateNode:
                    context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, ComponentsApi.RenderTreeBuilder.SetKey);
                    context.CodeWriter.Write(parameter.ParameterName);
                    context.CodeWriter.WriteEndMethodInvocation();
                    break;

                case ReferenceCaptureIntermediateNode capture:
                    context.CodeWriter.WriteStartInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, capture.IsComponentCapture ? ComponentsApi.RenderTreeBuilder.AddComponentReferenceCapture : ComponentsApi.RenderTreeBuilder.AddElementReferenceCapture);
                    context.CodeWriter.Write(parameter.SeqName);
                    context.CodeWriter.Write(", ");

                    var cast = capture.IsComponentCapture ? $"({capture.ComponentCaptureTypeName})" : string.Empty;
                    context.CodeWriter.Write($"(__value) => {{ {parameter.ParameterName}({cast}__value); }}");
                    context.CodeWriter.WriteEndMethodInvocation();
                    break;

                case CascadingGenericTypeParameter:
                    // We only use the synthetic cascading parameters for type inference
                    break;

                default:
                    throw new InvalidOperationException($"Not implemented: type inference method parameter from source {parameter.Source}");
            }
        }

        context.CodeWriter.WriteInstanceMethodInvocation(ComponentsApi.RenderTreeBuilder.BuilderParameter, ComponentsApi.RenderTreeBuilder.CloseComponent);

        writer.WriteLine("}");

        if (node.Component.Component.SuppliesCascadingGenericParameters())
        {
            // If this component cascades any generic parameters, we'll need to be able to capture its type inference
            // args at the call site. The point of this is to ensure that:
            //
            // [1] We only evaluate each expression once
            // [2] We evaluate them in the correct order matching the developer's source
            // [3] We can even make variables for lambdas or other expressions that can't just be assigned to implicitly-typed vars.
            //
            // We do that by emitting a method like the following. It has exactly the same generic type inference
            // characteristics as the regular CreateFoo_0 method emitted earlier
            //
            //  public static void CreateFoo_0_CaptureParameters<T1, T2>(T1 __arg0, out T1 __arg0_out, global::System.Collections.Generic.List<T2> __arg1, out global::System.Collections.Generic.List<T2> __arg1_out, int __seq2, string __arg2, out string __arg2_out)
            //  {
            //      __arg0_out = __arg0;
            //      __arg1_out = __arg1;
            //      __arg2_out = __arg2;
            //  }
            //
            writer.WriteLine();
            writer.Write("public static void ");
            writer.Write(node.MethodName);
            writer.Write("_CaptureParameters<");
            writer.Write(string.Join(", ", node.Component.Component.GetTypeParameters().Select(a => a.Name)));
            writer.Write(">");

            writer.Write("(");
            var isFirst = true;
            foreach (var parameter in parameters.Where(p => p.UsedForTypeInference))
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    writer.Write(", ");
                }

                writer.Write(parameter.TypeName);
                writer.Write(" ");
                writer.Write(parameter.ParameterName);
                writer.Write(", out ");
                writer.Write(parameter.TypeName);
                writer.Write(" ");
                writer.Write(parameter.ParameterName);
                writer.Write("_out");
            }

            writer.WriteLine(")");
            writer.WriteLine("{");
            foreach (var parameter in parameters.Where(p => p.UsedForTypeInference))
            {
                writer.Write("    ");
                writer.Write(parameter.ParameterName);
                writer.Write("_out = ");
                writer.Write(parameter.ParameterName);
                writer.WriteLine(";");
            }
            writer.WriteLine("}");
        }
    }

    protected List<TypeInferenceMethodParameter> GetTypeInferenceMethodParameters(ComponentTypeInferenceMethodIntermediateNode node)
    {
        var p = new List<TypeInferenceMethodParameter>();

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

                p.Add(new TypeInferenceMethodParameter($"__seq{p.Count}", typeName, $"__arg{p.Count}", usedForTypeInference: true, attribute));
            }
            else if (child is SplatIntermediateNode splat)
            {
                var typeName = ComponentsApi.AddMultipleAttributesTypeFullName;
                p.Add(new TypeInferenceMethodParameter($"__seq{p.Count}", typeName, $"__arg{p.Count}", usedForTypeInference: false, splat));
            }
        }

        foreach (var childContent in node.Component.ChildContents)
        {
            var typeName = childContent.TypeName;
            if (childContent.BoundAttribute != null && !childContent.BoundAttribute.IsGenericTypedProperty())
            {
                typeName = "global::" + typeName;
            }
            p.Add(new TypeInferenceMethodParameter($"__seq{p.Count}", typeName, $"__arg{p.Count}", usedForTypeInference: false, childContent));
        }

        foreach (var capture in node.Component.SetKeys)
        {
            p.Add(new TypeInferenceMethodParameter($"__seq{p.Count}", "object", $"__arg{p.Count}", usedForTypeInference: false, capture));
        }

        foreach (var capture in node.Component.Captures)
        {
            // The capture type name should already contain the global:: prefix.
            p.Add(new TypeInferenceMethodParameter($"__seq{p.Count}", capture.TypeName, $"__arg{p.Count}", usedForTypeInference: false, capture));
        }

        // Insert synthetic args for cascaded type inference at the start of the list
        // We do this last so that the indices above aren't affected
        if (node.ReceivesCascadingGenericTypes != null)
        {
            var i = 0;
            foreach (var cascadingGenericType in node.ReceivesCascadingGenericTypes)
            {
                p.Insert(i, new TypeInferenceMethodParameter(null, cascadingGenericType.ValueType, $"__syntheticArg{i}", usedForTypeInference: true, cascadingGenericType));
                i++;
            }
        }

        return p;
    }

    protected static void UseCapturedCascadingGenericParameterVariable(ComponentIntermediateNode node, TypeInferenceMethodParameter parameter, string variableName)
    {
        // If this captured variable corresponds to a generic type we want to cascade to
        // descendants, supply that info to descendants
        if (node.ProvidesCascadingGenericTypes != null)
        {
            foreach (var cascadeGeneric in node.ProvidesCascadingGenericTypes.Values)
            {
                if (cascadeGeneric.ValueSourceNode == parameter.Source)
                {
                    cascadeGeneric.ValueExpression = variableName;
                }
            }
        }

        // Since we've now evaluated and captured this expression, use the variable
        // instead of the expression from now on
        parameter.ReplaceSourceWithCapturedVariable(variableName);
    }

    protected class TypeInferenceMethodParameter
    {
        public string SeqName { get; private set; }
        public string TypeName { get; private set; }
        public string ParameterName { get; private set; }
        public bool UsedForTypeInference { get; private set; }
        public object Source { get; private set; }

        public TypeInferenceMethodParameter(string seqName, string typeName, string parameterName, bool usedForTypeInference, object source)
        {
            SeqName = seqName;
            TypeName = typeName;
            ParameterName = parameterName;
            UsedForTypeInference = usedForTypeInference;
            Source = source;
        }

        public void ReplaceSourceWithCapturedVariable(string variableName)
        {
            Source = new TypeInferenceCapturedVariable(variableName);
        }
    }

    protected class TypeInferenceCapturedVariable
    {
        public string VariableName { get; private set; }

        public TypeInferenceCapturedVariable(string variableName)
        {
            VariableName = variableName;
        }
    }
}
