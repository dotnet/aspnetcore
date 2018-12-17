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

        private List<DefaultAllowedChildTagDescriptorBuilder> _allowedChildTags;
        private List<DefaultBoundAttributeDescriptorBuilder> _attributeBuilders;
        private List<DefaultTagMatchingRuleDescriptorBuilder> _tagMatchingRuleBuilders;
        private RazorDiagnosticCollection _diagnostics;

        public DefaultTagHelperDescriptorBuilder(string kind, string name, string assemblyName)
        {
            Kind = kind;
            Name = name;
            AssemblyName = assemblyName;

            _metadata = new Dictionary<string, string>(StringComparer.Ordinal);

            // Tells code generation that these tag helpers are compatible with ITagHelper.
            // For now that's all we support.
            _metadata.Add(TagHelperMetadata.Runtime.Name, TagHelperConventions.DefaultKind);
        }

        public override string Name { get; }

        public override string AssemblyName { get; }

        public override string Kind { get; }

        public override string DisplayName { get; set; }

        public override string TagOutputHint { get; set; }

        public override string Documentation { get; set; }

        public override IDictionary<string, string> Metadata => _metadata;

        public override RazorDiagnosticCollection Diagnostics
        {
            get
            {
                if (_diagnostics == null)
                {
                    _diagnostics = new RazorDiagnosticCollection();
                }

                return _diagnostics;
            }
        }

        public override IReadOnlyList<AllowedChildTagDescriptorBuilder> AllowedChildTags
        {
            get
            {
                EnsureAllowedChildTags();

                return _allowedChildTags;
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

        public override void AllowChildTag(Action<AllowedChildTagDescriptorBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            EnsureAllowedChildTags();

            var builder = new DefaultAllowedChildTagDescriptorBuilder(this);
            configure(builder);
            _allowedChildTags.Add(builder);
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
            var diagnostics = new HashSet<RazorDiagnostic>();
            if (_diagnostics != null)
            {
                diagnostics.UnionWith(_diagnostics);
            }

            var allowedChildTags = Array.Empty<AllowedChildTagDescriptor>();
            if (_allowedChildTags != null)
            {
                var allowedChildTagsSet = new HashSet<AllowedChildTagDescriptor>(AllowedChildTagDescriptorComparer.Default);
                for (var i = 0; i < _allowedChildTags.Count; i++)
                {
                    allowedChildTagsSet.Add(_allowedChildTags[i].Build());
                }

                allowedChildTags = allowedChildTagsSet.ToArray();
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
                allowedChildTags,
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

        private void EnsureAllowedChildTags()
        {
            if (_allowedChildTags == null)
            {
                _allowedChildTags = new List<DefaultAllowedChildTagDescriptorBuilder>();
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
