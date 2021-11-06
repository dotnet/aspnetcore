// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

// This pass:
// 1. Adds diagnostics for missing generic type arguments
// 2. Rewrites the type name of the component to substitute generic type arguments
// 3. Rewrites the type names of parameters/child content to substitute generic type arguments
internal class ComponentGenericTypePass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    private TypeNameFeature _typeNameFeature;

    // Runs after components/eventhandlers/ref/bind/templates. We want to validate every component
    // and it's usage of ChildContent.
    public override int Order => 160;

    private TypeNameFeature TypeNameFeature
    {
        get
        {
            // Doing lazy intialization here to avoid making things really complicated when we don't
            // need to exercise this code in tests.
            if (_typeNameFeature == null)
            {
                _typeNameFeature = GetRequiredFeature<TypeNameFeature>();
            }

            return _typeNameFeature;
        }
    }

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        var visitor = new Visitor(this);
        visitor.Visit(documentNode);
    }

    private class Visitor : IntermediateNodeWalker
    {
        private readonly ComponentGenericTypePass _pass;

        // Incrementing ID for type inference method names
        private int _id;

        public Visitor(ComponentGenericTypePass pass)
        {
            _pass = pass;
        }

        public override void VisitComponent(ComponentIntermediateNode node)
        {
            if (node.Component.IsGenericTypedComponent())
            {
                // Not generic, ignore.
                Process(node);
            }

            base.VisitDefault(node);
        }

        private void Process(ComponentIntermediateNode node)
        {
            // First collect all of the information we have about each type parameter
            //
            // Listing all type parameters that exist
            var bindings = new Dictionary<string, Binding>();
            var componentTypeParameters = node.Component.GetTypeParameters().ToList();
            var supplyCascadingTypeParameters = componentTypeParameters
                .Where(p => p.IsCascadingTypeParameterProperty())
                .Select(p => p.Name)
                .ToList();
            foreach (var attribute in componentTypeParameters)
            {
                bindings.Add(attribute.Name, new Binding() { Attribute = attribute, });
            }

            // Listing all type arguments that have been specified.
            var hasTypeArgumentSpecified = false;
            foreach (var typeArgumentNode in node.TypeArguments)
            {
                hasTypeArgumentSpecified = true;

                var binding = bindings[typeArgumentNode.TypeParameterName];
                binding.Node = typeArgumentNode;
                binding.Content = GetContent(typeArgumentNode);

                // Offer this explicit type argument to descendants too
                if (supplyCascadingTypeParameters.Contains(typeArgumentNode.TypeParameterName))
                {
                    node.ProvidesCascadingGenericTypes ??= new();
                    node.ProvidesCascadingGenericTypes[typeArgumentNode.TypeParameterName] = new CascadingGenericTypeParameter
                    {
                        GenericTypeNames = new[] { typeArgumentNode.TypeParameterName },
                        ValueType = typeArgumentNode.TypeParameterName,
                        ValueExpression = $"default({binding.Content})",
                    };
                }
            }

            if (hasTypeArgumentSpecified)
            {
                // OK this means that the developer has specified at least one type parameter.
                // Either they specified everything and its OK to rewrite, or its an error.
                if (ValidateTypeArguments(node, bindings))
                {
                    var mappings = bindings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Content);
                    RewriteTypeNames(_pass.TypeNameFeature.CreateGenericTypeRewriter(mappings), node);
                }

                return;
            }

            // OK if we get here that means that no type arguments were specified, so we will try to infer
            // the type.
            //
            // The actual inference is done by the C# compiler, we just emit an a method that represents the
            // use of this component.

            // Since we're generating code in a different namespace, we need to 'global qualify' all of the types
            // to avoid clashes with our generated code.
            RewriteTypeNames(_pass.TypeNameFeature.CreateGlobalQualifiedTypeNameRewriter(bindings.Keys), node);

            //
            // We need to verify that an argument was provided that 'covers' each type parameter.
            //
            // For example, consider a repeater where the generic type is the 'item' type, but the developer has
            // not set the items. We won't be able to do type inference on this and so it will just be nonsense.
            foreach (var attribute in node.Attributes)
            {
                if (attribute != null && TryFindGenericTypeNames(attribute.BoundAttribute, out var typeParameters))
                {
                    var attributeValueIsLambda = _pass.TypeNameFeature.IsLambda(GetContent(attribute));
                    var provideCascadingGenericTypes = new CascadingGenericTypeParameter
                    {
                        GenericTypeNames = typeParameters,
                        ValueType = attribute.BoundAttribute.TypeName,
                        ValueSourceNode = attribute,
                    };

                    foreach (var typeName in typeParameters)
                    {
                        if (supplyCascadingTypeParameters.Contains(typeName))
                        {
                            // Advertise that this particular inferred generic type is available to descendants.
                            // There might be multiple sources for each generic type, so pick the one that has the
                            // fewest other generic types on it. For example if we could infer from either List<T>
                            // or Dictionary<T, U>, we prefer List<T>.
                            node.ProvidesCascadingGenericTypes ??= new();
                            if (!node.ProvidesCascadingGenericTypes.TryGetValue(typeName, out var existingValue)
                                || existingValue.GenericTypeNames.Count > typeParameters.Count)
                            {
                                node.ProvidesCascadingGenericTypes[typeName] = provideCascadingGenericTypes;
                            }
                        }

                        if (attributeValueIsLambda)
                        {
                            // For attributes whose values are lambdas, we don't know whether or not the value
                            // covers the generic type - it depends on the content of the lambda.
                            // For example, "() => 123" can cover Func<T>, but "() => null" cannot. So we'll
                            // accept cascaded generic types from ancestors if they are compatible with the lambda,
                            // hence we don't remove it from the list of uncovered generic types until after
                            // we try matching against ancestor cascades.
                            if (bindings.TryGetValue(typeName, out var binding))
                            {
                                binding.CoveredByLambda = true;
                            }
                        }
                        else
                        {
                            bindings.Remove(typeName);
                        }
                    }
                }
            }

            // For any remaining bindings, scan up the hierarchy of ancestor components and try to match them
            // with a cascaded generic parameter that can cover this one
            List<CascadingGenericTypeParameter> receivesCascadingGenericTypes = null;
            foreach (var uncoveredBindingKey in bindings.Keys.ToList())
            {
                var uncoveredBinding = bindings[uncoveredBindingKey];
                foreach (var candidateAncestor in Ancestors.OfType<ComponentIntermediateNode>())
                {
                    if (candidateAncestor.ProvidesCascadingGenericTypes != null
                        && candidateAncestor.ProvidesCascadingGenericTypes.TryGetValue(uncoveredBindingKey, out var genericTypeProvider))
                    {
                        // If the parameter value is an expression that includes multiple generic types, we only want
                        // to use it if we want *all* those generic types. That is, a parameter of type MyType<T0, T1>
                        // can supply types to a Child<T0, T1>, but not to a Child<T0>.
                        // This is purely to avoid blowing up the complexity of the implementation here and could be
                        // overcome in the future if we want. We'd need to figure out which extra types are unwanted,
                        // and rewrite them to some unique name, and add that to the generic parameters list of the
                        // inference methods.
                        if (genericTypeProvider.GenericTypeNames.All(GenericTypeIsUsed))
                        {
                            bindings.Remove(uncoveredBindingKey);
                            receivesCascadingGenericTypes ??= new();
                            receivesCascadingGenericTypes.Add(genericTypeProvider);

                            // It's sufficient to identify the closest provider for each type parameter
                            break;
                        }

                        bool GenericTypeIsUsed(string typeName) => componentTypeParameters
                            .Select(t => t.Name)
                            .Contains(typeName, StringComparer.Ordinal);
                    }
                }
            }

            // There are two remaining sources of possible generic type info which we consider
            // lower-priority than cascades from ancestors. Since these two sources *may* actually
            // resolve generic type ambiguities in some cases, we treat them as covering.
            //
            // [1] Attributes given as lambda expressions. These are lower priority than ancestor
            //     cascades because in most cases, lambdas don't provide type info
            foreach (var entryToRemove in bindings.Where(e => e.Value.CoveredByLambda).ToList())
            {
                // Treat this binding as covered, because it's possible that the lambda does provide
                // enough info for type inference to succeed.
                bindings.Remove(entryToRemove.Key);
            }

            // [2] Child content parameters, which are nearly always defined as untyped lambdas
            //     (at least, that's what the Razor compiler produces), but can technically be
            //     hardcoded as a RenderFragment<Something> and hence actually give type info.
            foreach (var attribute in node.ChildContents)
            {
                if (TryFindGenericTypeNames(attribute.BoundAttribute, out var typeParameters))
                {
                    foreach (var typeName in typeParameters)
                    {
                        bindings.Remove(typeName);
                    }
                }
            }

            // If any bindings remain then this means we would never be able to infer the arguments of this
            // component usage because the user hasn't set properties that include all of the types.
            if (bindings.Count > 0)
            {
                // However we still want to generate 'type inference' code because we want the errors to be as
                // helpful as possible. So let's substitute 'object' for all of those type parameters, and add
                // an error.
                var mappings = bindings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Content);
                RewriteTypeNames(_pass.TypeNameFeature.CreateGenericTypeRewriter(mappings), node);

                node.Diagnostics.Add(ComponentDiagnosticFactory.Create_GenericComponentTypeInferenceUnderspecified(node.Source, node, node.Component.GetTypeParameters()));
            }

            // Next we need to generate a type inference 'method' node. This represents a method that we will codegen that
            // contains all of the operations on the render tree building. Calling a method to operate on the builder
            // will allow the C# compiler to perform type inference.
            var documentNode = (DocumentIntermediateNode)Ancestors[Ancestors.Count - 1];
            CreateTypeInferenceMethod(documentNode, node, receivesCascadingGenericTypes);
        }

        private bool TryFindGenericTypeNames(BoundAttributeDescriptor boundAttribute, out IReadOnlyList<string> typeParameters)
        {
            if (boundAttribute == null)
            {
                // Will be null for attributes set on the component that don't match a declared component parameter
                typeParameters = null;
                return false;
            }

            if (!boundAttribute.IsGenericTypedProperty())
            {
                typeParameters = null;
                return false;
            }

            // Now we need to parse the type name and extract the generic parameters.
            // Two cases;
            // 1. name is a simple identifier like TItem
            // 2. name contains type parameters like Dictionary<string, TItem>
            typeParameters = _pass.TypeNameFeature.ParseTypeParameters(boundAttribute.TypeName);
            if (typeParameters.Count == 0)
            {
                typeParameters = new[] { boundAttribute.TypeName };
            }

            return true;
        }

        private string GetContent(ComponentTypeArgumentIntermediateNode node)
        {
            return string.Join(string.Empty, node.FindDescendantNodes<IntermediateToken>().Where(t => t.IsCSharp).Select(t => t.Content));
        }

        private string GetContent(ComponentAttributeIntermediateNode node)
        {
            return string.Join(string.Empty, node.FindDescendantNodes<IntermediateToken>().Where(t => t.IsCSharp).Select(t => t.Content));
        }

        private static bool ValidateTypeArguments(ComponentIntermediateNode node, Dictionary<string, Binding> bindings)
        {
            var missing = new List<BoundAttributeDescriptor>();
            foreach (var binding in bindings)
            {
                if (binding.Value.Node == null || string.IsNullOrWhiteSpace(binding.Value.Content))
                {
                    missing.Add(binding.Value.Attribute);
                }
            }

            if (missing.Count > 0)
            {
                // We add our own error for this because its likely the user will see other errors due
                // to incorrect codegen without the types. Our errors message will pretty clearly indicate
                // what to do, whereas the other errors might be confusing.
                node.Diagnostics.Add(ComponentDiagnosticFactory.Create_GenericComponentMissingTypeArgument(node.Source, node, missing));
                return false;
            }

            return true;
        }

        private void RewriteTypeNames(TypeNameRewriter rewriter, ComponentIntermediateNode node)
        {
            // Rewrite the component type name
            node.TypeName = rewriter.Rewrite(node.TypeName);

            foreach (var attribute in node.Attributes)
            {
                string globallyQualifiedTypeName = null;

                if (attribute.TypeName != null)
                {
                    globallyQualifiedTypeName = rewriter.Rewrite(attribute.TypeName);
                    attribute.GloballyQualifiedTypeName = globallyQualifiedTypeName;
                }

                if (attribute.BoundAttribute?.IsGenericTypedProperty() ?? false && attribute.TypeName != null)
                {
                    // If we know the type name, then replace any generic type parameter inside it with
                    // the known types.
                    attribute.TypeName = globallyQualifiedTypeName;
                }
                else if (attribute.TypeName == null && (attribute.BoundAttribute?.IsDelegateProperty() ?? false))
                {
                    // This is a weakly typed delegate, treat it as Action<object>
                    attribute.TypeName = "System.Action<System.Object>";
                }
                else if (attribute.TypeName == null && (attribute.BoundAttribute?.IsEventCallbackProperty() ?? false))
                {
                    // This is a weakly typed event-callback, treat it as EventCallback (non-generic)
                    attribute.TypeName = ComponentsApi.EventCallback.FullTypeName;
                }
                else if (attribute.TypeName == null)
                {
                    // This is a weakly typed attribute, treat it as System.Object
                    attribute.TypeName = "System.Object";
                }
            }

            foreach (var capture in node.Captures)
            {
                if (capture.IsComponentCapture && capture.ComponentCaptureTypeName != null)
                {
                    capture.ComponentCaptureTypeName = rewriter.Rewrite(capture.ComponentCaptureTypeName);
                }
                else if (capture.IsComponentCapture)
                {
                    capture.ComponentCaptureTypeName = "System.Object";
                }
            }

            foreach (var childContent in node.ChildContents)
            {
                if (childContent.BoundAttribute?.IsGenericTypedProperty() ?? false && childContent.TypeName != null)
                {
                    // If we know the type name, then replace any generic type parameter inside it with
                    // the known types.
                    childContent.TypeName = rewriter.Rewrite(childContent.TypeName);
                }
                else if (childContent.IsParameterized)
                {
                    // This is a non-generic parameterized child content like RenderFragment<int>, leave it as is.
                }
                else
                {
                    // This is a weakly typed child content, treat it as RenderFragment
                    childContent.TypeName = ComponentsApi.RenderFragment.FullTypeName;
                }
            }
        }

        private void CreateTypeInferenceMethod(DocumentIntermediateNode documentNode, ComponentIntermediateNode node, List<CascadingGenericTypeParameter> receivesCascadingGenericTypes)
        {
            var @namespace = documentNode.FindPrimaryNamespace().Content;
            @namespace = string.IsNullOrEmpty(@namespace) ? "__Blazor" : "__Blazor." + @namespace;
            @namespace += "." + documentNode.FindPrimaryClass().ClassName;

            var genericTypeConstraints = node.Component.BoundAttributes
                .Where(t => t.Metadata.ContainsKey(ComponentMetadata.Component.TypeParameterConstraintsKey))
                .Select(t => t.Metadata[ComponentMetadata.Component.TypeParameterConstraintsKey]);

            var typeInferenceNode = new ComponentTypeInferenceMethodIntermediateNode()
            {
                Component = node,

                // Method name is generated and guaranteed not to collide, since it's unique for each
                // component call site.
                MethodName = $"Create{CSharpIdentifier.SanitizeIdentifier(node.TagName)}_{_id++}",
                FullTypeName = @namespace + ".TypeInference",

                ReceivesCascadingGenericTypes = receivesCascadingGenericTypes,
                GenericTypeConstraints = genericTypeConstraints
            };

            node.TypeInferenceNode = typeInferenceNode;

            // Now we need to insert the type inference node into the tree.
            var namespaceNode = documentNode.Children
                .OfType<NamespaceDeclarationIntermediateNode>()
                .Where(n => n.Annotations.Contains(new KeyValuePair<object, object>(ComponentMetadata.Component.GenericTypedKey, bool.TrueString)))
                .FirstOrDefault();
            if (namespaceNode == null)
            {
                namespaceNode = new NamespaceDeclarationIntermediateNode()
                {
                    Annotations =
                        {
                            { ComponentMetadata.Component.GenericTypedKey, bool.TrueString },
                        },
                    Content = @namespace,
                };

                documentNode.Children.Add(namespaceNode);
            }

            var classNode = namespaceNode.Children
                .OfType<ClassDeclarationIntermediateNode>()
                .Where(n => n.ClassName == "TypeInference")
                .FirstOrDefault();
            if (classNode == null)
            {
                classNode = new ClassDeclarationIntermediateNode()
                {
                    ClassName = "TypeInference",
                    Modifiers =
                        {
                            "internal",
                            "static",
                        },
                };
                namespaceNode.Children.Add(classNode);
            }

            classNode.Children.Add(typeInferenceNode);
        }
    }

    private class Binding
    {
        public BoundAttributeDescriptor Attribute { get; set; }

        public string Content { get; set; }

        public ComponentTypeArgumentIntermediateNode Node { get; set; }

        public bool CoveredByLambda { get; set; }
    }
}
