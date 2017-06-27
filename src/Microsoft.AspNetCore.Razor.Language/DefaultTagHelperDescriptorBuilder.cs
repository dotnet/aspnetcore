// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultTagHelperDescriptorBuilder : TagHelperDescriptorBuilder
    {
        // Required values
        private readonly string _kind;
        private readonly string _name;
        private readonly string _assemblyName;
        private readonly Dictionary<string, string> _metadata;

        private string _displayName;
        private string _documentation;
        private string _tagOutputHint;
        private HashSet<string> _allowedChildTags;
        private List<DefaultBoundAttributeDescriptorBuilder> _attributeBuilders;
        private List<DefaultTagMatchingRuleDescriptorBuilder> _tagMatchingRuleBuilders;
        private HashSet<RazorDiagnostic> _diagnostics;

        public DefaultTagHelperDescriptorBuilder(string kind, string name, string assemblyName)
        {
            _kind = kind;
            _name = name;
            _assemblyName = assemblyName;

            _metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public IDictionary<string, string> Metadata => _metadata;

        public override TagHelperDescriptorBuilder BindAttribute(Action<BoundAttributeDescriptorBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            EnsureAttributeBuilders();

            var builder = new DefaultBoundAttributeDescriptorBuilder(this, _kind);
            configure(builder);
            _attributeBuilders.Add(builder);
            return this;
        }

        public override TagHelperDescriptorBuilder TagMatchingRule(Action<TagMatchingRuleDescriptorBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            EnsureTagMatchingRuleBuilders();

            var builder = new DefaultTagMatchingRuleDescriptorBuilder();
            configure(builder);
            _tagMatchingRuleBuilders.Add(builder);

            return this;
        }

        public override TagHelperDescriptorBuilder AllowChildTag(string allowedChild)
        {
            EnsureAllowedChildTags();
            _allowedChildTags.Add(allowedChild);

            return this;
        }

        public override TagHelperDescriptorBuilder TagOutputHint(string hint)
        {
            _tagOutputHint = hint;

            return this;
        }

        public override TagHelperDescriptorBuilder Documentation(string documentation)
        {
            _documentation = documentation;

            return this;
        }

        public override TagHelperDescriptorBuilder AddMetadata(string key, string value)
        {
            _metadata[key] = value;

            return this;
        }

        public override TagHelperDescriptorBuilder AddDiagnostic(RazorDiagnostic diagnostic)
        {
            EnsureDiagnostics();
            _diagnostics.Add(diagnostic);

            return this;
        }

        public override TagHelperDescriptorBuilder DisplayName(string displayName)
        {
            if (displayName == null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            _displayName = displayName;

            return this;
        }

        public override TagHelperDescriptorBuilder TypeName(string typeName)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            _metadata[TagHelperMetadata.Common.TypeName] = typeName;
            return this;
        }

        public override TagHelperDescriptor Build()
        {
            var validationDiagnostics = Validate();
            var diagnostics = new HashSet<RazorDiagnostic>(validationDiagnostics);
            if (_diagnostics != null)
            {
                diagnostics.UnionWith(_diagnostics);
            }

            var tagMatchingRules = Array.Empty<TagMatchingRuleDescriptor>();
            if (_tagMatchingRuleBuilders != null)
            {
                var tagMatchingRuleSet = new HashSet<TagMatchingRuleDescriptor>(TagMatchingRuleDescriptorComparer.Default);
                for (var i = 0; i < _tagMatchingRuleBuilders.Count; i++)
                {
                    tagMatchingRuleSet.Add(_tagMatchingRuleBuilders[i].Build());
                }

                tagMatchingRules = tagMatchingRuleSet.ToArray();
            }

            var attributes = Array.Empty<BoundAttributeDescriptor>();
            if (_attributeBuilders != null)
            {
                var attributeSet = new HashSet<BoundAttributeDescriptor>(BoundAttributeDescriptorComparer.Default);
                for (var i = 0; i < _attributeBuilders.Count; i++)
                {
                    attributeSet.Add(_attributeBuilders[i].Build());
                }

                attributes = attributeSet.ToArray();
            }

            var descriptor = new DefaultTagHelperDescriptor(
                _kind,
                _name,
                _assemblyName,
                GetDisplayName(),
                _documentation,
                _tagOutputHint,
                tagMatchingRules,
                attributes,
                _allowedChildTags?.ToArray() ?? Array.Empty<string>(),
                new Dictionary<string, string>(_metadata),
                diagnostics.ToArray());

            return descriptor;
        }

        public override void Reset()
        {
            _documentation = null;
            _tagOutputHint = null;
            _allowedChildTags?.Clear();
            _attributeBuilders?.Clear();
            _tagMatchingRuleBuilders?.Clear();
            _metadata.Clear();
            _diagnostics?.Clear();
        }

        public string GetDisplayName()
        {
            if (_displayName != null)
            {
                return _displayName;
            }

            return _metadata.ContainsKey(TagHelperMetadata.Common.TypeName) ? _metadata[TagHelperMetadata.Common.TypeName] : _name;
        }

        private IEnumerable<RazorDiagnostic> Validate()
        {
            if (_allowedChildTags != null)
            {
                foreach (var name in _allowedChildTags)
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRestrictedChildNullOrWhitespace(GetDisplayName());

                        yield return diagnostic;
                    }
                    else if (name != TagHelperMatchingConventions.ElementCatchAllName)
                    {
                        foreach (var character in name)
                        {
                            if (char.IsWhiteSpace(character) || HtmlConventions.InvalidNonWhitespaceHtmlCharacters.Contains(character))
                            {
                                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRestrictedChild(GetDisplayName(), name, character);

                                yield return diagnostic;
                            }
                        }
                    }
                }
            }
        }

        private void EnsureAttributeBuilders()
        {
            if (_attributeBuilders == null)
            {
                _attributeBuilders = new List<DefaultBoundAttributeDescriptorBuilder>();
            }
        }

        private void EnsureTagMatchingRuleBuilders()
        {
            if (_tagMatchingRuleBuilders == null)
            {
                _tagMatchingRuleBuilders = new List<DefaultTagMatchingRuleDescriptorBuilder>();
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
    }
}
