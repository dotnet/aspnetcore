// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    internal class TagHelperDescriptorJsonConverter : JsonConverter
    {
        public static readonly TagHelperDescriptorJsonConverter Instance = new TagHelperDescriptorJsonConverter();

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return typeof(TagHelperDescriptor).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            var descriptor = JObject.Load(reader);
            var descriptorKind = descriptor[nameof(TagHelperDescriptor.Kind)].Value<string>();
            var typeName = descriptor[nameof(TagHelperDescriptor.Name)].Value<string>();
            var assemblyName = descriptor[nameof(TagHelperDescriptor.AssemblyName)].Value<string>();
            var tagMatchingRules = descriptor[nameof(TagHelperDescriptor.TagMatchingRules)].Value<JArray>();
            var boundAttributes = descriptor[nameof(TagHelperDescriptor.BoundAttributes)].Value<JArray>();
            var childTags = descriptor[nameof(TagHelperDescriptor.AllowedChildTags)].Value<JArray>();
            var documentation = descriptor[nameof(TagHelperDescriptor.Documentation)].Value<string>();
            var tagOutputHint = descriptor[nameof(TagHelperDescriptor.TagOutputHint)].Value<string>();
            var diagnostics = descriptor[nameof(TagHelperDescriptor.Diagnostics)].Value<JArray>();
            var metadata = descriptor[nameof(TagHelperDescriptor.Metadata)].Value<JObject>();

            var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);

            builder.Documentation = documentation;
            builder.TagOutputHint = tagOutputHint;

            foreach (var tagMatchingRule in tagMatchingRules)
            {
                var rule = tagMatchingRule.Value<JObject>();
                builder.TagMatchingRule(b => ReadTagMatchingRule(b, rule, serializer));
            }

            foreach (var boundAttribute in boundAttributes)
            {
                var attribute = boundAttribute.Value<JObject>();
                builder.BindAttribute(b => ReadBoundAttribute(b, attribute, serializer));
            }

            foreach (var childTag in childTags)
            {
                var tag = childTag.Value<JObject>();
                builder.AllowChildTag(childTagBuilder => ReadAllowedChildTag(childTagBuilder, tag, serializer));
            }

            foreach (var diagnostic in diagnostics)
            {
                var diagnosticReader = diagnostic.CreateReader();
                var diagnosticObject = serializer.Deserialize<RazorDiagnostic>(diagnosticReader);
                builder.Diagnostics.Add(diagnosticObject);
            }

            var metadataReader = metadata.CreateReader();
            var metadataValue = serializer.Deserialize<Dictionary<string, string>>(metadataReader);
            foreach (var item in metadataValue)
            {
                builder.Metadata[item.Key] = item.Value;
            }

            return builder.Build();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // We should never get here because CanWrite returns false.
            // We want the default serializer to handle TagHelperDescriptor serialization.
            throw new NotImplementedException();
        }

        private void ReadTagMatchingRule(TagMatchingRuleDescriptorBuilder builder, JObject rule, JsonSerializer serializer)
        {
            var tagName = rule[nameof(TagMatchingRuleDescriptor.TagName)].Value<string>();
            var attributes = rule[nameof(TagMatchingRuleDescriptor.Attributes)].Value<JArray>();
            var parentTag = rule[nameof(TagMatchingRuleDescriptor.ParentTag)].Value<string>();
            var tagStructure = rule[nameof(TagMatchingRuleDescriptor.TagStructure)].Value<int>();
            var diagnostics = rule[nameof(TagMatchingRuleDescriptor.Diagnostics)].Value<JArray>();

            builder.TagName = tagName;
            builder.ParentTag = parentTag;
            builder.TagStructure = (TagStructure)tagStructure;

            foreach (var attribute in attributes)
            {
                var attibuteValue = attribute.Value<JObject>();
                builder.Attribute(b => ReadRequiredAttribute(b, attibuteValue, serializer));
            }

            foreach (var diagnostic in diagnostics)
            {
                var diagnosticReader = diagnostic.CreateReader();
                var diagnosticObject = serializer.Deserialize<RazorDiagnostic>(diagnosticReader);
                builder.Diagnostics.Add(diagnosticObject);
            }
        }

        private void ReadRequiredAttribute(RequiredAttributeDescriptorBuilder builder, JObject attribute, JsonSerializer serializer)
        {
            var name = attribute[nameof(RequiredAttributeDescriptor.Name)].Value<string>();
            var nameComparison = attribute[nameof(RequiredAttributeDescriptor.NameComparison)].Value<int>();
            var value = attribute[nameof(RequiredAttributeDescriptor.Value)].Value<string>();
            var valueComparison = attribute[nameof(RequiredAttributeDescriptor.ValueComparison)].Value<int>();
            var diagnostics = attribute[nameof(RequiredAttributeDescriptor.Diagnostics)].Value<JArray>();

            builder.Name = name;
            builder.NameComparisonMode = (RequiredAttributeDescriptor.NameComparisonMode)nameComparison;
            builder.Value = value;
            builder.ValueComparisonMode = (RequiredAttributeDescriptor.ValueComparisonMode)valueComparison;

            foreach (var diagnostic in diagnostics)
            {
                var diagnosticReader = diagnostic.CreateReader();
                var diagnosticObject = serializer.Deserialize<RazorDiagnostic>(diagnosticReader);
                builder.Diagnostics.Add(diagnosticObject);
            }
        }

        private void ReadAllowedChildTag(AllowedChildTagDescriptorBuilder builder, JObject childTag, JsonSerializer serializer)
        {
            var name = childTag[nameof(AllowedChildTagDescriptor.Name)].Value<string>();
            var displayName = childTag[nameof(AllowedChildTagDescriptor.DisplayName)].Value<string>();
            var diagnostics = childTag[nameof(AllowedChildTagDescriptor.Diagnostics)].Value<JArray>();

            builder.Name = name;
            builder.DisplayName = displayName;

            foreach (var diagnostic in diagnostics)
            {
                var diagnosticReader = diagnostic.CreateReader();
                var diagnosticObject = serializer.Deserialize<RazorDiagnostic>(diagnosticReader);
                builder.Diagnostics.Add(diagnosticObject);
            }
        }

        private void ReadBoundAttribute(BoundAttributeDescriptorBuilder builder, JObject attribute, JsonSerializer serializer)
        {
            var descriptorKind = attribute[nameof(BoundAttributeDescriptor.Kind)].Value<string>();
            var name = attribute[nameof(BoundAttributeDescriptor.Name)].Value<string>();
            var typeName = attribute[nameof(BoundAttributeDescriptor.TypeName)].Value<string>();
            var isEnum = attribute[nameof(BoundAttributeDescriptor.IsEnum)].Value<bool>();
            var indexerNamePrefix = attribute[nameof(BoundAttributeDescriptor.IndexerNamePrefix)].Value<string>();
            var indexerTypeName = attribute[nameof(BoundAttributeDescriptor.IndexerTypeName)].Value<string>();
            var documentation = attribute[nameof(BoundAttributeDescriptor.Documentation)].Value<string>();
            var diagnostics = attribute[nameof(BoundAttributeDescriptor.Diagnostics)].Value<JArray>();
            var metadata = attribute[nameof(BoundAttributeDescriptor.Metadata)].Value<JObject>();

            builder.Name = name;
            builder.TypeName = typeName;
            builder.Documentation = documentation;

            if (indexerNamePrefix != null)
            {
                builder.AsDictionary(indexerNamePrefix, indexerTypeName);
            }

            if (isEnum)
            {
                builder.IsEnum = true;
            }

            foreach (var diagnostic in diagnostics)
            {
                var diagnosticReader = diagnostic.CreateReader();
                var diagnosticObject = serializer.Deserialize<RazorDiagnostic>(diagnosticReader);
                builder.Diagnostics.Add(diagnosticObject);
            }

            var metadataReader = metadata.CreateReader();
            var metadataValue = serializer.Deserialize<Dictionary<string, string>>(metadataReader);
            foreach (var item in metadataValue)
            {
                builder.Metadata[item.Key] = item.Value;
            }
        }
    }
}
