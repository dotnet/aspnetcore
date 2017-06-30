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
        private readonly Dictionary<string, string> _metadata;

        private HashSet<string> _allowedChildTags;
        private List<DefaultBoundAttributeDescriptorBuilder> _attributeBuilders;
        private List<DefaultTagMatchingRuleDescriptorBuilder> _tagMatchingRuleBuilders;
        private DefaultRazorDiagnosticCollection _diagnostics;

        public DefaultTagHelperDescriptorBuilder(string kind, string name, string assemblyName)
        {
            Kind = kind;
            Name = name;
            AssemblyName = assemblyName;

            _metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public override string Name { get; }

        public override string AssemblyName { get; }

        public override string Kind { get; }

        public override string DisplayName { get; set; }

        public override ICollection<string> AllowedChildTags
        {
            get
            {
                if (_allowedChildTags == null)
                {
                    _allowedChildTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                return _allowedChildTags;
            }
        }

        public override string TagOutputHint { get; set; }

        public override string Documentation { get; set; }

        public override IDictionary<string, string> Metadata => _metadata;

        public override RazorDiagnosticCollection Diagnostics
        {
            get
            {
                if (_diagnostics == null)
                {
                    _diagnostics = new DefaultRazorDiagnosticCollection();
                }

                return _diagnostics;
            }
        }

        public override IReadOnlyList<BoundAttributeDescriptorBuilder> BoundAttributes
        {
            get
            {
                EnsureAttributeBuilders();

                return _attributeBuilders;
            }
        }

        public override IReadOnlyList<TagMatchingRuleDescriptorBuilder> TagMatchingRules
        {
            get
            {
                EnsureTagMatchingRuleBuilders();

                return _tagMatchingRuleBuilders;
            }
        }

        public override void BindAttribute(Action<BoundAttributeDescriptorBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            EnsureAttributeBuilders();

            var builder = new DefaultBoundAttributeDescriptorBuilder(this, Kind);
            configure(builder);
            _attributeBuilders.Add(builder);
        }

        public override void TagMatchingRule(Action<TagMatchingRuleDescriptorBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            EnsureTagMatchingRuleBuilders();

            var builder = new DefaultTagMatchingRuleDescriptorBuilder();
            configure(builder);
            _tagMatchingRuleBuilders.Add(builder);
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
                Kind,
                Name,
                AssemblyName,
                GetDisplayName(),
                Documentation,
                TagOutputHint,
                tagMatchingRules,
                attributes,
                _allowedChildTags?.ToArray() ?? Array.Empty<string>(),
                new Dictionary<string, string>(_metadata),
                diagnostics.ToArray());

            return descriptor;
        }

        public override void Reset()
        {
            Documentation = null;
            TagOutputHint = null;
            _allowedChildTags?.Clear();
            _attributeBuilders?.Clear();
            _tagMatchingRuleBuilders?.Clear();
            _metadata.Clear();
            _diagnostics?.Clear();
        }

        public string GetDisplayName()
        {
            if (DisplayName != null)
            {
                return DisplayName;
            }

            return this.GetTypeName() ?? Name;
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
    }
}
