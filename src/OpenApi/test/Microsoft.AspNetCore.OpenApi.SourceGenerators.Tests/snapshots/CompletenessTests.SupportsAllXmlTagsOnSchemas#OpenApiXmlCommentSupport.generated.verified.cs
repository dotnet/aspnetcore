﻿//HintName: OpenApiXmlCommentSupport.generated.cs
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#nullable enable
// Suppress warnings about obsolete types and members
// in generated code
#pragma warning disable CS0612, CS0618

namespace System.Runtime.CompilerServices
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.OpenApi.SourceGenerators, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    file sealed class InterceptsLocationAttribute : System.Attribute
    {
        public InterceptsLocationAttribute(int version, string data)
        {
        }
    }
}

namespace Microsoft.AspNetCore.OpenApi.Generated
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.OpenApi;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.OpenApi.Models;
    using Microsoft.OpenApi.Models.Interfaces;
    using Microsoft.OpenApi.Models.References;
    using Microsoft.OpenApi.Any;

    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.OpenApi.SourceGenerators, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file record XmlComment(
        string? Summary,
        string? Description,
        string? Remarks,
        string? Returns,
        string? Value,
        bool Deprecated,
        List<string>? Examples,
        List<XmlParameterComment>? Parameters,
        List<XmlResponseComment>? Responses);

    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.OpenApi.SourceGenerators, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file record XmlParameterComment(string? Name, string? Description, string? Example, bool Deprecated);

    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.OpenApi.SourceGenerators, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file record XmlResponseComment(string Code, string? Description, string? Example);

    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.OpenApi.SourceGenerators, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file static class XmlCommentCache
    {
        private static Dictionary<string, XmlComment>? _cache;
        public static Dictionary<string, XmlComment> Cache => _cache ??= GenerateCacheEntries();

        private static Dictionary<string, XmlComment> GenerateCacheEntries()
        {
            var cache = new Dictionary<string, XmlComment>();

            cache.Add(@"T:ExampleClass", new XmlComment(@"Every class and member should have a one sentence
summary describing its purpose.", null, @"     You can expand on that one sentence summary to
     provide more information for readers. In this case,
     the `ExampleClass` provides different C#
     elements to show how you would add documentation
    comments for most elements in a typical class.
     The remarks can add multiple paragraphs, so you can
write detailed information for developers that use
your work. You should add everything needed for
readers to be successful. This class contains
examples for the following:
     * Summary

This should provide a one sentence summary of the class or member.
* Remarks

This is typically a more detailed description of the class or member
* para

The para tag separates a section into multiple paragraphs
* list

Provides a list of terms or elements
* returns, param

Used to describe parameters and return values
* value
Used to describe properties
* exception

Used to describe exceptions that may be thrown
* c, cref, see, seealso

These provide code style and links to other
documentation elements
* example, code

These are used for code examples
     The list above uses the ""table"" style. You could
also use the ""bullet"" or ""number"" style. Neither
would typically use the ""term"" element.

Note: paragraphs are double spaced. Use the *br*
tag for single spaced lines.", null, null, false, null, null, null));
            cache.Add(@"T:Person", new XmlComment(@"This is an example of a positional record.", null, @"There isn't a way to add XML comments for properties
created for positional records, yet. The language
design team is still considering what tags should
be supported, and where. Currently, you can use
the ""param"" tag to describe the parameters to the
primary constructor.", null, null, false, null, [new XmlParameterComment(@"FirstName", @"This tag will apply to the primary constructor parameter.", null, false), new XmlParameterComment(@"LastName", @"This tag will apply to the primary constructor parameter.", null, false)], null));
            cache.Add(@"T:MainClass", new XmlComment(@"A summary about this class.", null, @"These remarks would explain more about this class.
In this example, these comments also explain the
general information about the derived class.", null, null, false, null, null, null));
            cache.Add(@"T:DerivedClass", new XmlComment(@"A summary about this class.", null, @"These remarks would explain more about this class.
In this example, these comments also explain the
general information about the derived class.", null, null, false, null, null, null));
            cache.Add(@"T:ITestInterface", new XmlComment(@"This interface would describe all the methods in
its contract.", null, @"While elided for brevity, each method or property
in this interface would contain docs that you want
to duplicate in each implementing class.", null, null, false, null, null, null));
            cache.Add(@"T:ImplementingClass", new XmlComment(@"This interface would describe all the methods in
its contract.", null, @"While elided for brevity, each method or property
in this interface would contain docs that you want
to duplicate in each implementing class.", null, null, false, null, null, null));
            cache.Add(@"T:InheritOnlyReturns", new XmlComment(@"This class shows hows you can ""inherit"" the doc
comments from one method in another method.", null, @"You can inherit all comments, or only a specific tag,
represented by an xpath expression.", null, null, false, null, null, null));
            cache.Add(@"T:InheritAllButRemarks", new XmlComment(@"This class shows an example of sharing comments across methods.", null, null, null, null, false, null, null, null));
            cache.Add(@"T:GenericClass`1", new XmlComment(@"This is a generic class.", null, @"This example shows how to specify the GenericClass&lt;T&gt;
type as a cref attribute.
In generic classes and methods, you'll often want to reference the
generic type, or the type parameter.", null, null, false, null, null, null));
            cache.Add(@"T:GenericParent", new XmlComment(@"This class validates the behavior for mapping
generic types to open generics for use in
typeof expressions.", null, null, null, null, false, null, null, null));
            cache.Add(@"T:ParamsAndParamRefs", new XmlComment(@"This shows examples of typeparamref and typeparam tags", null, null, null, null, false, null, null, null));
            cache.Add(@"P:ExampleClass.Label", new XmlComment(null, null, @"    The string? ExampleClass.Label is a <see langword=""string"" />
    that you use for a label.
    Note that there isn't a way to provide a ""cref"" to
each accessor, only to the property itself.", null, @"The `Label` property represents a label
for this instance.", false, null, null, null));
            cache.Add(@"P:Person.FirstName", new XmlComment(@"This tag will apply to the primary constructor parameter.", null, null, null, null, false, null, null, null));
            cache.Add(@"P:Person.LastName", new XmlComment(@"This tag will apply to the primary constructor parameter.", null, null, null, null, false, null, null, null));
            cache.Add(@"P:GenericParent.Id", new XmlComment(@"This property is a nullable value type.", null, null, null, null, false, null, null, null));
            cache.Add(@"P:GenericParent.Name", new XmlComment(@"This property is a nullable reference type.", null, null, null, null, false, null, null, null));
            cache.Add(@"P:GenericParent.TaskOfTupleProp", new XmlComment(@"This property is a generic type containing a tuple.", null, null, null, null, false, null, null, null));
            cache.Add(@"P:GenericParent.TupleWithGenericProp", new XmlComment(@"This property is a tuple with a generic type inside.", null, null, null, null, false, null, null, null));
            cache.Add(@"P:GenericParent.TupleWithNestedGenericProp", new XmlComment(@"This property is a tuple with a nested generic type inside.", null, null, null, null, false, null, null, null));
            cache.Add(@"M:ExampleClass.Add(System.Int32,System.Int32)~System.Int32", new XmlComment(@"Adds two integers and returns the result.", null, null, @"The sum of two integers.", null, false, [@"    ```int c = Math.Add(4, 5);
if (c &gt; 10)
{
    Console.WriteLine(c);
}```"], [new XmlParameterComment(@"left", @"The left operand of the addition.", null, false), new XmlParameterComment(@"right", @"The right operand of the addition.", null, false)], null));
            cache.Add(@"M:ExampleClass.AddAsync(System.Int32,System.Int32)~System.Threading.Tasks.Task{System.Int32}", new XmlComment(@"This method is an example of a method that
returns an awaitable item.", null, null, null, null, false, null, null, null));
            cache.Add(@"M:ExampleClass.DoNothingAsync~System.Threading.Tasks.Task", new XmlComment(@"This method is an example of a method that
returns a Task which should map to a void return type.", null, null, null, null, false, null, null, null));
            cache.Add(@"M:ExampleClass.AddNumbers(System.Int32[])~System.Int32", new XmlComment(@"This method is an example of a method that consumes
an params array.", null, null, null, null, false, null, null, null));
            cache.Add(@"M:ITestInterface.Method(System.Int32)~System.Int32", new XmlComment(@"This method is part of the test interface.", null, @"This content would be inherited by classes
that implement this interface when the
implementing class uses ""inheritdoc""", @"The value of arg", null, false, null, [new XmlParameterComment(@"arg", @"The argument to the method", null, false)], null));
            cache.Add(@"M:InheritOnlyReturns.MyParentMethod(System.Boolean)~System.Boolean", new XmlComment(@"In this example, this summary is only visible for this method.", null, null, @"A boolean", null, false, null, null, null));
            cache.Add(@"M:InheritOnlyReturns.MyChildMethod~System.Boolean", new XmlComment(null, null, null, @"A boolean", null, false, null, null, null));
            cache.Add(@"M:InheritAllButRemarks.MyParentMethod(System.Boolean)~System.Boolean", new XmlComment(@"In this example, this summary is visible on all the methods.", null, @"The remarks can be inherited by other methods
using the xpath expression.", @"A boolean", null, false, null, null, null));
            cache.Add(@"M:InheritAllButRemarks.MyChildMethod~System.Boolean", new XmlComment(@"In this example, this summary is visible on all the methods.", null, null, @"A boolean", null, false, null, null, null));
            cache.Add(@"M:GenericParent.GetTaskOfTuple~System.Threading.Tasks.Task{System.ValueTuple{System.Int32,System.String}}", new XmlComment(@"This method returns a generic type containing a tuple.", null, null, null, null, false, null, null, null));
            cache.Add(@"M:GenericParent.GetTupleOfTask~System.ValueTuple{System.Int32,System.Collections.Generic.Dictionary{System.Int32,System.String}}", new XmlComment(@"This method returns a tuple with a generic type inside.", null, null, null, null, false, null, null, null));
            cache.Add(@"M:GenericParent.GetTupleOfTask1``1~System.ValueTuple{System.Int32,System.Collections.Generic.Dictionary{System.Int32,``0}}", new XmlComment(@"This method return a tuple with a generic type containing a
type parameter inside.", null, null, null, null, false, null, null, null));
            cache.Add(@"M:GenericParent.GetTupleOfTask2``1~System.ValueTuple{``0,System.Collections.Generic.Dictionary{System.Int32,System.String}}", new XmlComment(@"This method return a tuple with a generic type containing a
type parameter inside.", null, null, null, null, false, null, null, null));
            cache.Add(@"M:GenericParent.GetNestedGeneric~System.Collections.Generic.Dictionary{System.Int32,System.Collections.Generic.Dictionary{System.Int32,System.String}}", new XmlComment(@"This method returns a nested generic with all types resolved.", null, null, null, null, false, null, null, null));
            cache.Add(@"M:GenericParent.GetNestedGeneric1``1~System.Collections.Generic.Dictionary{System.Int32,System.Collections.Generic.Dictionary{System.Int32,``0}}", new XmlComment(@"This method returns a nested generic with a type parameter.", null, null, null, null, false, null, null, null));
            cache.Add(@"M:ParamsAndParamRefs.GetGenericValue``1(``0)~``0", new XmlComment(@"The GetGenericValue method.", null, @"This sample shows how to specify the T ParamsAndParamRefs.GetGenericValue&lt;T&gt;(T para)
method as a cref attribute.
The parameter and return value are both of an arbitrary type,
T", null, null, false, null, null, null));

            return cache;
        }
    }

    file static class DocumentationCommentIdHelper
    {
        /// <summary>
        /// Generates a documentation comment ID for a type.
        /// Example: T:Namespace.Outer+Inner`1 becomes T:Namespace.Outer.Inner`1
        /// </summary>
        public static string CreateDocumentationId(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return "T:" + GetTypeDocId(type, includeGenericArguments: false, omitGenericArity: false);
        }

        /// <summary>
        /// Generates a documentation comment ID for a property.
        /// Example: P:Namespace.ContainingType.PropertyName or for an indexer P:Namespace.ContainingType.Item(System.Int32)
        /// </summary>
        public static string CreateDocumentationId(this PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            var sb = new StringBuilder();
            sb.Append("P:");

            if (property.DeclaringType != null)
            {
                sb.Append(GetTypeDocId(property.DeclaringType, includeGenericArguments: false, omitGenericArity: false));
            }

            sb.Append('.');
            sb.Append(property.Name);

            // For indexers, include the parameter list.
            var indexParams = property.GetIndexParameters();
            if (indexParams.Length > 0)
            {
                sb.Append('(');
                for (int i = 0; i < indexParams.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(GetTypeDocId(indexParams[i].ParameterType, includeGenericArguments: true, omitGenericArity: false));
                }
                sb.Append(')');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a documentation comment ID for a method (or constructor).
        /// For example:
        ///   M:Namespace.ContainingType.MethodName(ParamType1,ParamType2)~ReturnType
        ///   M:Namespace.ContainingType.#ctor(ParamType)
        /// </summary>
        public static string CreateDocumentationId(this MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            var sb = new StringBuilder();
            sb.Append("M:");

            // Append the fully qualified name of the declaring type.
            if (method.DeclaringType != null)
            {
                sb.Append(GetTypeDocId(method.DeclaringType, includeGenericArguments: false, omitGenericArity: false));
            }

            sb.Append('.');

            // Append the method name, handling constructors specially.
            if (method.IsConstructor)
            {
                sb.Append(method.IsStatic ? "#cctor" : "#ctor");
            }
            else
            {
                sb.Append(method.Name);
                if (method.IsGenericMethod)
                {
                    sb.Append("``");
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}", method.GetGenericArguments().Length);
                }
            }

            // Append the parameter list, if any.
            var parameters = method.GetParameters();
            if (parameters.Length > 0)
            {
                sb.Append('(');
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    // Omit the generic arity for the parameter type.
                    sb.Append(GetTypeDocId(parameters[i].ParameterType, includeGenericArguments: true, omitGenericArity: true));
                }
                sb.Append(')');
            }

            // Append the return type after a '~' (if the method returns a value).
            if (method.ReturnType != typeof(void))
            {
                sb.Append('~');
                // Omit the generic arity for the return type.
                sb.Append(GetTypeDocId(method.ReturnType, includeGenericArguments: true, omitGenericArity: true));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a documentation ID string for a type.
        /// This method handles nested types (replacing '+' with '.'),
        /// generic types, arrays, pointers, by-ref types, and generic parameters.
        /// The <paramref name="includeGenericArguments"/> flag controls whether
        /// constructed generic type arguments are emitted, while <paramref name="omitGenericArity"/>
        /// controls whether the generic arity marker (e.g. "`1") is appended.
        /// </summary>
        private static string GetTypeDocId(Type type, bool includeGenericArguments, bool omitGenericArity)
        {
            if (type.IsGenericParameter)
            {
                // Use `` for method-level generic parameters and ` for type-level.
                if (type.DeclaringMethod != null)
                {
                    return "``" + type.GenericParameterPosition;
                }
                else if (type.DeclaringType != null)
                {
                    return "`" + type.GenericParameterPosition;
                }
                else
                {
                    return type.Name;
                }
            }

            if (type.IsGenericType)
            {
                Type genericDef = type.GetGenericTypeDefinition();
                string fullName = genericDef.FullName ?? genericDef.Name;

                var sb = new StringBuilder(fullName.Length);

                // Replace '+' with '.' for nested types
                for (var i = 0; i < fullName.Length; i++)
                {
                    char c = fullName[i];
                    if (c == '+')
                    {
                        sb.Append('.');
                    }
                    else if (c == '`')
                    {
                        break;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                if (!omitGenericArity)
                {
                    int arity = genericDef.GetGenericArguments().Length;
                    sb.Append('`');
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}", arity);
                }

                if (includeGenericArguments && !type.IsGenericTypeDefinition)
                {
                    var typeArgs = type.GetGenericArguments();
                    sb.Append('{');

                    for (int i = 0; i < typeArgs.Length; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(',');
                        }

                        sb.Append(GetTypeDocId(typeArgs[i], includeGenericArguments, omitGenericArity));
                    }

                    sb.Append('}');
                }

                return sb.ToString();
            }

            // For non-generic types, use FullName (if available) and replace nested type separators.
            return (type.FullName ?? type.Name).Replace('+', '.');
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.OpenApi.SourceGenerators, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file class XmlCommentOperationTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            var methodInfo = context.Description.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor
                ? controllerActionDescriptor.MethodInfo
                : context.Description.ActionDescriptor.EndpointMetadata.OfType<MethodInfo>().SingleOrDefault();

            if (methodInfo is null)
            {
                return Task.CompletedTask;
            }
            if (XmlCommentCache.Cache.TryGetValue(methodInfo.CreateDocumentationId(), out var methodComment))
            {
                if (methodComment.Summary is { } summary)
                {
                    operation.Summary = summary;
                }
                if (methodComment.Description is { } description)
                {
                    operation.Description = description;
                }
                if (methodComment.Remarks is { } remarks)
                {
                    operation.Description = remarks;
                }
                if (methodComment.Parameters is { Count: > 0})
                {
                    foreach (var parameterComment in methodComment.Parameters)
                    {
                        var parameterInfo = methodInfo.GetParameters().SingleOrDefault(info => info.Name == parameterComment.Name);
                        var operationParameter = operation.Parameters?.SingleOrDefault(parameter => parameter.Name == parameterComment.Name);
                        if (operationParameter is not null)
                        {
                            var targetOperationParameter = UnwrapOpenApiParameter(operationParameter);
                            targetOperationParameter.Description = parameterComment.Description;
                            if (parameterComment.Example is { } jsonString)
                            {
                                targetOperationParameter.Example = jsonString.Parse();
                            }
                            targetOperationParameter.Deprecated = parameterComment.Deprecated;
                        }
                        else
                        {
                            var requestBody = operation.RequestBody;
                            if (requestBody is not null)
                            {
                                requestBody.Description = parameterComment.Description;
                                if (parameterComment.Example is { } jsonString)
                                {
                                    var content = requestBody?.Content?.Values;
                                    if (content is null)
                                    {
                                        continue;
                                    }
                                    foreach (var mediaType in content)
                                    {
                                        mediaType.Example = jsonString.Parse();
                                    }
                                }
                            }
                        }
                    }
                }
                if (methodComment.Responses is { Count: > 0} && operation.Responses is { Count: > 0 })
                {
                    foreach (var response in operation.Responses)
                    {
                        var responseComment = methodComment.Responses.SingleOrDefault(xmlResponse => xmlResponse.Code == response.Key);
                        if (responseComment is not null)
                        {
                            response.Value.Description = responseComment.Description;
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        private static OpenApiParameter UnwrapOpenApiParameter(IOpenApiParameter sourceParameter)
        {
            if (sourceParameter is OpenApiParameterReference parameterReference)
            {
                if (parameterReference.Target is OpenApiParameter target)
                {
                    return target;
                }
                else
                {
                    throw new InvalidOperationException("The input schema must be an OpenApiParameter or OpenApiParameterReference.");
                }
            }
            else if (sourceParameter is OpenApiParameter directParameter)
            {
                return directParameter;
            }
            else
            {
                throw new InvalidOperationException("The input schema must be an OpenApiParameter or OpenApiParameterReference.");
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.OpenApi.SourceGenerators, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file class XmlCommentSchemaTransformer : IOpenApiSchemaTransformer
    {
        public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            if (context.JsonPropertyInfo is { AttributeProvider: PropertyInfo propertyInfo })
            {
                if (XmlCommentCache.Cache.TryGetValue(propertyInfo.CreateDocumentationId(), out var propertyComment))
                {
                    schema.Description = propertyComment.Value ?? propertyComment.Returns ?? propertyComment.Summary;
                    if (propertyComment.Examples?.FirstOrDefault() is { } jsonString)
                    {
                        schema.Example = jsonString.Parse();
                    }
                }
            }
            if (XmlCommentCache.Cache.TryGetValue(context.JsonTypeInfo.Type.CreateDocumentationId(), out var typeComment))
            {
                schema.Description = typeComment.Summary;
                if (typeComment.Examples?.FirstOrDefault() is { } jsonString)
                {
                    schema.Example = jsonString.Parse();
                }
            }
            return Task.CompletedTask;
        }
    }

    file static class JsonNodeExtensions
    {
        public static JsonNode? Parse(this string? json)
        {
            if (json is null)
            {
                return null;
            }

            try
            {
                return JsonNode.Parse(json);
            }
            catch (JsonException)
            {
                try
                {
                    // If parsing fails, try wrapping in quotes to make it a valid JSON string
                    return JsonNode.Parse($"\"{json.Replace("\"", "\\\"")}\"");
                }
                catch (JsonException)
                {
                    return null;
                }
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.AspNetCore.OpenApi.SourceGenerators, Version=42.42.42.42, Culture=neutral, PublicKeyToken=adb9793829ddae60", "42.42.42.42")]
    file static class GeneratedServiceCollectionExtensions
    {
        [InterceptsLocation]
        public static IServiceCollection AddOpenApi(this IServiceCollection services)
        {
            return services.AddOpenApi("v1", options =>
            {
                options.AddSchemaTransformer(new XmlCommentSchemaTransformer());
                options.AddOperationTransformer(new XmlCommentOperationTransformer());
            });
        }

    }
}