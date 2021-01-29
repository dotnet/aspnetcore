// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
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
                foreach (var attribute in node.Component.GetTypeParameters())
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

                    // Offer this type argument to descendants too
                    // TODO: Only offer type args that are explicitly opted-in to cascading
                    node.ProvidesCascadingGenericTypes ??= new();
                    node.ProvidesCascadingGenericTypes[typeArgumentNode.TypeParameterName] = new CascadingGenericTypeParameter
                    {
                        GenericTypeName = typeArgumentNode.TypeParameterName,
                        ValueType = typeArgumentNode.TypeParameterName,
                        ValueExpression = $"default({binding.Content})",
                    };
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
                    foreach (var typeName in FindGenericTypeNames(attribute.BoundAttribute))
                    {
                        bindings.Remove(typeName);

                        // Advertise that this particular generic type is available to descendants
                        // TODO: Only include ones explicitly opted-in to cascading
                        node.ProvidesCascadingGenericTypes ??= new();
                        node.ProvidesCascadingGenericTypes[typeName] = new CascadingGenericTypeParameter
                        {
                            GenericTypeName = typeName,
                            ValueType = attribute.BoundAttribute.TypeName,
                            ValueSourceNode = attribute,
                        };
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
                            && candidateAncestor.ProvidesCascadingGenericTypes.TryGetValue(uncoveredBindingKey, out var receiveArg))
                        {
                            bindings.Remove(uncoveredBindingKey);
                            receivesCascadingGenericTypes ??= new();
                            receivesCascadingGenericTypes.Add(receiveArg);
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

            private IEnumerable<string> FindGenericTypeNames(BoundAttributeDescriptor boundAttribute)
            {
                if (boundAttribute == null)
                {
                    // Will be null for attributes set on the component that don't match a declared component parameter
                    yield break;
                }

                // Now we need to parse the type name and extract the generic parameters.
                //
                // Two cases;
                // 1. name is a simple identifier like TItem
                // 2. name contains type parameters like Dictionary<string, TItem>
                if (!boundAttribute.IsGenericTypedProperty())
                {
                    yield break;
                }

                // TODO: Avoid returning type parameters if the value for the associated expression
                // is a type-inferred lambda (e.g., (a, b) => { ... }). Such values don't contribute
                // to determining the generic types, and if those are all we have, we need not to
                // remove the 'binding' entry because then we wouldn't receive anything cascaded
                // from ancestors.

                var typeParameters = _pass.TypeNameFeature.ParseTypeParameters(boundAttribute.TypeName);
                if (typeParameters.Count == 0)
                {
                    yield return boundAttribute.TypeName;
                }
                else
                {
                    for (var i = 0; i < typeParameters.Count; i++)
                    {
                        var typeParameter = typeParameters[i];
                        yield return typeParameter.ToString();
                    }
                }
            }

            private string GetContent(ComponentTypeArgumentIntermediateNode node)
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

                var typeInferenceNode = new ComponentTypeInferenceMethodIntermediateNode()
                {
                    Component = node,

                    // Method name is generated and guaranteed not to collide, since it's unique for each
                    // component call site.
                    MethodName = $"Create{CSharpIdentifier.SanitizeIdentifier(node.TagName)}_{_id++}",
                    FullTypeName = @namespace + ".TypeInference",

                    ReceivesCascadingGenericTypes = receivesCascadingGenericTypes,
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
        }
    }
}
