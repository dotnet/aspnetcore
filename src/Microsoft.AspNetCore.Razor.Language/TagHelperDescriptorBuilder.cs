// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    public sealed class TagHelperDescriptorBuilder
    {
        public static readonly string DescriptorKind = "ITagHelper";
        public static readonly string TypeNameKey = "ITagHelper.TypeName";

        private static ICollection<char> InvalidNonWhitespaceAllowedChildCharacters { get; } = new HashSet<char>(
            new[] { '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'', '*' });


        private string _documentation;
        private string _tagOutputHint;
        private HashSet<string> _allowedChildTags;
        private HashSet<BoundAttributeDescriptor> _attributeDescriptors;
        private HashSet<TagMatchingRule> _tagMatchingRules;
        private readonly string _assemblyName;
        private readonly string _typeName;
        private readonly Dictionary<string, string> _metadata;
        private HashSet<RazorDiagnostic> _diagnostics;

        private TagHelperDescriptorBuilder(string typeName, string assemblyName)
        {
            _typeName = typeName;
            _assemblyName = assemblyName;
            _metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public static TagHelperDescriptorBuilder Create(string typeName, string assemblyName)
        {
            return new TagHelperDescriptorBuilder(typeName, assemblyName);
        }

        public TagHelperDescriptorBuilder BindAttribute(BoundAttributeDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            EnsureAttributeDescriptors();
            _attributeDescriptors.Add(descriptor);

            return this;
        }

        public TagHelperDescriptorBuilder BindAttribute(Action<TagHelperBoundAttributeDescriptorBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = TagHelperBoundAttributeDescriptorBuilder.Create(_typeName);

            configure(builder);

            var attributeDescriptor = builder.Build();

            return BindAttribute(attributeDescriptor);
        }

        public TagHelperDescriptorBuilder TagMatchingRule(TagMatchingRule rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }

            EnsureTagMatchingRules();
            _tagMatchingRules.Add(rule);

            return this;
        }

        public TagHelperDescriptorBuilder TagMatchingRule(Action<TagMatchingRuleBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = TagMatchingRuleBuilder.Create();

            configure(builder);

            var rule = builder.Build();

            return TagMatchingRule(rule);
        }

        public TagHelperDescriptorBuilder AllowChildTag(string allowedChild)
        {
            EnsureAllowedChildTags();
            _allowedChildTags.Add(allowedChild);

            return this;
        }

        public TagHelperDescriptorBuilder TagOutputHint(string hint)
        {
            _tagOutputHint = hint;

            return this;
        }

        public TagHelperDescriptorBuilder Documentation(string documentation)
        {
            _documentation = documentation;

            return this;
        }

        public TagHelperDescriptorBuilder AddMetadata(string key, string value)
        {
            _metadata[key] = value;

            return this;
        }

        public TagHelperDescriptorBuilder AddDiagnostic(RazorDiagnostic diagnostic)
        {
            EnsureDiagnostics();
            _diagnostics.Add(diagnostic);

            return this;
        }

        public TagHelperDescriptor Build()
        {
            var validationDiagnostics = Validate();
            var diagnostics = new HashSet<RazorDiagnostic>(validationDiagnostics);
            if (_diagnostics != null)
            {
                diagnostics.UnionWith(_diagnostics);
            }

            var descriptor = new ITagHelperDescriptor(
                _typeName,
                _assemblyName,
                _typeName /* Name */,
                _typeName /* DisplayName */,
                _documentation,
                _tagOutputHint,
                _tagMatchingRules ?? Enumerable.Empty<TagMatchingRule>(),
                _attributeDescriptors ?? Enumerable.Empty<BoundAttributeDescriptor>(),
                _allowedChildTags ?? Enumerable.Empty<string>(),
                _metadata,
                diagnostics);

            return descriptor;
        }

        public void Reset()
        {
            _documentation = null;
            _tagOutputHint = null;
            _allowedChildTags?.Clear();
            _attributeDescriptors?.Clear();
            _tagMatchingRules?.Clear();
            _metadata.Clear();
            _diagnostics?.Clear();
        }

        private IEnumerable<RazorDiagnostic> Validate()
        {
            if (_allowedChildTags != null)
            {
                foreach (var name in _allowedChildTags)
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRestrictedChildNullOrWhitespace(_typeName);

                        yield return diagnostic;
                    }
                    else if (name != TagHelperMatchingConventions.ElementCatchAllName)
                    {
                        foreach (var character in name)
                        {
                            if (char.IsWhiteSpace(character) || InvalidNonWhitespaceAllowedChildCharacters.Contains(character))
                            {
                                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRestrictedChild(name, _typeName, character);

                                yield return diagnostic;
                            }
                        }
                    }
                }
            }
        }

        private void EnsureAttributeDescriptors()
        {
            if (_attributeDescriptors == null)
            {
                _attributeDescriptors = new HashSet<BoundAttributeDescriptor>(BoundAttributeDescriptorComparer.Default);
            }
        }

        private void EnsureTagMatchingRules()
        {
            if (_tagMatchingRules == null)
            {
                _tagMatchingRules = new HashSet<TagMatchingRule>(TagMatchingRuleComparer.Default);
            }
        }

        private void EnsureAllowedChildTags()
        {
            if (_allowedChildTags == null)
            {
                _allowedChildTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private void EnsureDiagnostics()
        {
            if (_diagnostics == null)
            {
                _diagnostics = new HashSet<RazorDiagnostic>();
            }
        }

        private class ITagHelperDescriptor : TagHelperDescriptor
        {
            public ITagHelperDescriptor(
                string typeName,
                string assemblyName,
                string name,
                string displayName,
                string documentation,
                string tagOutputHint,
                IEnumerable<TagMatchingRule> tagMatchingRules,
                IEnumerable<BoundAttributeDescriptor> attributeDescriptors,
                IEnumerable<string> allowedChildTags,
                Dictionary<string, string> metadata,
                IEnumerable<RazorDiagnostic> diagnostics) : base(DescriptorKind)
            {
                Name = typeName;
                AssemblyName = assemblyName;
                DisplayName = displayName;
                Documentation = documentation;
                TagOutputHint = tagOutputHint;
                TagMatchingRules = new List<TagMatchingRule>(tagMatchingRules);
                BoundAttributes = new List<BoundAttributeDescriptor>(attributeDescriptors);
                AllowedChildTags = new List<string>(allowedChildTags);
                Diagnostics = new List<RazorDiagnostic>(diagnostics);
                Metadata = new Dictionary<string, string>(metadata)
                {
                    [TypeNameKey] = typeName
                };
            }
        }
    }
}
