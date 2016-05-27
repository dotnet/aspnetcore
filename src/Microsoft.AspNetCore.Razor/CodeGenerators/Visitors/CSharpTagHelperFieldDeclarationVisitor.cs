// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.CodeGenerators.Visitors
{
    public class CSharpTagHelperFieldDeclarationVisitor : CodeVisitor<CSharpCodeWriter>
    {
        private const string _preAllocatedAttributeVariablePrefix = "__tagHelperAttribute_";
        private readonly HashSet<string> _declaredTagHelpers;
        private readonly Dictionary<TagHelperAttributeKey, string> _preAllocatedAttributes;
        private readonly GeneratedTagHelperContext _tagHelperContext;
        private bool _foundTagHelpers;

        public CSharpTagHelperFieldDeclarationVisitor(
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
            : base(writer, context)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _declaredTagHelpers = new HashSet<string>(StringComparer.Ordinal);
            _tagHelperContext = Context.Host.GeneratedClassContext.GeneratedTagHelperContext;
            _preAllocatedAttributes = new Dictionary<TagHelperAttributeKey, string>();
        }

        protected override void Visit(TagHelperChunk chunk)
        {
            // We only want to setup tag helper manager fields if there are tag helpers, and only once
            if (!_foundTagHelpers)
            {
                _foundTagHelpers = true;

                // We want to hide declared TagHelper fields so they cannot be stepped over via a debugger.
                Writer.WriteLineHiddenDirective();

                // Runtime fields aren't useful during design time.
                if (!Context.Host.DesignTimeMode)
                {
                    // Need to disable the warning "X is assigned to but never used." for the value buffer since
                    // whether it's used depends on how a TagHelper is used.
                    Writer.WritePragma("warning disable 0414");
                    Writer
                        .Write("private ")
                        .WriteVariableDeclaration(
                            "string", 
                            CSharpTagHelperCodeRenderer.StringValueBufferVariableName,
                            value: null);
                    Writer.WritePragma("warning restore 0414");

                    WritePrivateField(
                        _tagHelperContext.ExecutionContextTypeName,
                        CSharpTagHelperCodeRenderer.ExecutionContextVariableName,
                        value: null);

                    WritePrivateField(
                        _tagHelperContext.RunnerTypeName,
                        CSharpTagHelperCodeRenderer.RunnerVariableName,
                        value: null);

                    WritePrivateField(
                        _tagHelperContext.ScopeManagerTypeName,
                        CSharpTagHelperCodeRenderer.ScopeManagerVariableName,
                        value: null);
                }
            }

            foreach (var descriptor in chunk.Descriptors)
            {
                if (!_declaredTagHelpers.Contains(descriptor.TypeName))
                {
                    _declaredTagHelpers.Add(descriptor.TypeName);

                    WritePrivateField(
                        descriptor.TypeName,
                        CSharpTagHelperCodeRenderer.GetVariableName(descriptor),
                        value: null);
                }
            }

            if (!Context.Host.DesignTimeMode)
            {
                PreAllocateTagHelperAttributes(chunk);
            }

            // We need to dive deeper to ensure we pick up any nested tag helpers.
            Accept(chunk.Children);
        }

        private void PreAllocateTagHelperAttributes(TagHelperChunk chunk)
        {
            var boundAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < chunk.Attributes.Count; i++)
            {
                var attribute = chunk.Attributes[i];
                var associatedAttributeDescriptors = chunk.Descriptors.SelectMany(descriptor => descriptor.Attributes)
                    .Where(attributeDescriptor => attributeDescriptor.IsNameMatch(attribute.Name));

                // If there's no descriptors associated or is a repeated attribute with same name as a bound attribute, 
                // it is considered as an unbound attribute.
                var isUnBoundAttribute = !associatedAttributeDescriptors.Any() || !boundAttributes.Add(attribute.Name);

                // Perf: We will preallocate TagHelperAttribute for unbound attributes and simple bound string valued attributes.
                if (isUnBoundAttribute || CanPreallocateBoundAttribute(associatedAttributeDescriptors, attribute))
                {
                    string preAllocatedAttributeVariableName = null;

                    if (attribute.ValueStyle == HtmlAttributeValueStyle.Minimized)
                    {
                        Debug.Assert(attribute.Value == null);

                        var preAllocatedAttributeKey = new TagHelperAttributeKey(
                            attribute.Name, 
                            value: null, 
                            unBoundAttribute: isUnBoundAttribute, 
                            valueStyle: attribute.ValueStyle);
                        if (TryCachePreallocatedVariableName(preAllocatedAttributeKey, out preAllocatedAttributeVariableName))
                        {
                            Writer
                                .Write("private static readonly global::")
                                .Write(_tagHelperContext.TagHelperAttributeTypeName)
                                .Write(" ")
                                .Write(preAllocatedAttributeVariableName)
                                .Write(" = ")
                                .WriteStartNewObject("global::" + _tagHelperContext.TagHelperAttributeTypeName)
                                .WriteStringLiteral(attribute.Name)
                                .WriteEndMethodInvocation();
                        }
                    }
                    else
                    {
                        Debug.Assert(attribute.Value != null);

                        string plainText;
                        if (CSharpTagHelperCodeRenderer.TryGetPlainTextValue(attribute.Value, out plainText))
                        {
                            var preAllocatedAttributeKey = new TagHelperAttributeKey(attribute.Name, plainText, isUnBoundAttribute, attribute.ValueStyle);
                            if (TryCachePreallocatedVariableName(preAllocatedAttributeKey, out preAllocatedAttributeVariableName))
                            {
                                Writer
                                    .Write("private static readonly global::")
                                    .Write(_tagHelperContext.TagHelperAttributeTypeName)
                                    .Write(" ")
                                    .Write(preAllocatedAttributeVariableName)
                                    .Write(" = ")
                                    .WriteStartNewObject("global::" + _tagHelperContext.TagHelperAttributeTypeName)
                                    .WriteStringLiteral(attribute.Name)
                                    .WriteParameterSeparator();

                                if (isUnBoundAttribute)
                                {
                                    // For unbound attributes, we need to create HtmlString.
                                    Writer
                                        .WriteStartNewObject("global::" + _tagHelperContext.EncodedHtmlStringTypeName)
                                        .WriteStringLiteral(plainText)
                                        .WriteEndMethodInvocation(endLine: false);
                                }
                                else
                                {
                                    Writer.WriteStringLiteral(plainText);
                                    
                                }

                                Writer
                                    .WriteParameterSeparator()
                                    .Write($"global::{typeof(HtmlAttributeValueStyle).FullName}.{attribute.ValueStyle}")
                                    .WriteEndMethodInvocation();
                            }
                        }
                    }

                    if (preAllocatedAttributeVariableName != null)
                    {
                        chunk.Attributes[i] = new TagHelperAttributeTracker(
                            attribute.Name,
                            new PreallocatedTagHelperAttributeChunk
                            {
                                AttributeVariableAccessor = preAllocatedAttributeVariableName
                            },
                            attribute.ValueStyle);
                    }
                }
            }
        }

        private static bool CanPreallocateBoundAttribute(
            IEnumerable<TagHelperAttributeDescriptor> associatedAttributeDescriptors,
            TagHelperAttributeTracker attribute)
        {
            // If the attribute value is a Dynamic value, it cannot be preallocated.
            if (CSharpTagHelperCodeRenderer.IsDynamicAttributeValue(attribute.Value))
            {
                return false;
            }

            // Only attributes that are associated with string typed properties can be preallocated.
            var attributeName = attribute.Name;
            var allStringProperties = associatedAttributeDescriptors
                .All(attributeDescriptor => attributeDescriptor.IsStringProperty);

            return allStringProperties;
        }

        public override void Accept(Chunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            var parentChunk = chunk as ParentChunk;

            // If we're any ParentChunk other than TagHelperChunk then we want to dive into its Children
            // to search for more TagHelperChunk chunks. This if-statement enables us to not override
            // each of the special ParentChunk types and then dive into their children.
            if (parentChunk != null && !(parentChunk is TagHelperChunk))
            {
                Accept(parentChunk.Children);
            }
            else
            {
                // If we're a TagHelperChunk or any other non ParentChunk we ".Accept" it. This ensures
                // that our overridden Visit(TagHelperChunk) method gets called and is not skipped over.
                // If we're a non ParentChunk or a TagHelperChunk then we want to just invoke the Visit
                // method for that given chunk (base.Accept indirectly calls the Visit method).
                base.Accept(chunk);
            }
        }

        private bool TryCachePreallocatedVariableName(TagHelperAttributeKey key, out string preAllocatedAttributeVariableName)
        {
            if (!_preAllocatedAttributes.TryGetValue(key, out preAllocatedAttributeVariableName))
            {
                preAllocatedAttributeVariableName = _preAllocatedAttributeVariablePrefix + _preAllocatedAttributes.Count;
                _preAllocatedAttributes[key] = preAllocatedAttributeVariableName;
                return true;
            }

            return false;
        }

        private void WritePrivateField(string type, string name, string value)
        {
            Writer
                .Write("private global::")
                .WriteVariableDeclaration(type, name, value);
        }

        private struct TagHelperAttributeKey : IEquatable<TagHelperAttributeKey>
        {
            public TagHelperAttributeKey(string name, string value, bool unBoundAttribute, HtmlAttributeValueStyle valueStyle)
            {
                Name = name;
                Value = value;
                UnBoundAttribute = unBoundAttribute;
                ValueStyle = valueStyle;
            }

            public string Name { get; }

            public string Value { get; }

            public bool UnBoundAttribute { get; }

            public HtmlAttributeValueStyle ValueStyle { get; }

            public override int GetHashCode()
            {
                var hashCodeCombiner = HashCodeCombiner.Start();
                hashCodeCombiner.Add(Name, StringComparer.Ordinal);
                hashCodeCombiner.Add(Value, StringComparer.Ordinal);
                hashCodeCombiner.Add(UnBoundAttribute);
                hashCodeCombiner.Add(ValueStyle);

                return hashCodeCombiner.CombinedHash;
            }

            public override bool Equals(object obj)
            {
                var other = obj as TagHelperAttributeKey?;

                if (other != null)
                {
                    return Equals(other.Value);
                }

                return false;
            }

            public bool Equals(TagHelperAttributeKey other)
            {
                return string.Equals(Name, other.Name, StringComparison.Ordinal) &&
                    string.Equals(Value, other.Value, StringComparison.Ordinal) &&
                    UnBoundAttribute == other.UnBoundAttribute &&
                    ValueStyle == other.ValueStyle;
            }
        }
    }
}