// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public sealed class TagMatchingRuleBuilder
    {
        private static ICollection<char> InvalidNonWhitespaceTagNameCharacters { get; } = new HashSet<char>(
            new[] { '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'', '*' });

        private string _tagName;
        private string _parentTag;
        private TagStructure _tagStructure;
        private HashSet<RequiredAttributeDescriptor> _requiredAttributeDescriptors;
        private HashSet<RazorDiagnostic> _diagnostics;

        private TagMatchingRuleBuilder()
        {
        }

        public static TagMatchingRuleBuilder Create()
        {
            return new TagMatchingRuleBuilder();
        }

        public TagMatchingRuleBuilder RequireTagName(string tagName)
        {
            _tagName = tagName;

            return this;
        }

        public TagMatchingRuleBuilder RequireParentTag(string parentTag)
        {
            _parentTag = parentTag;

            return this;
        }

        public TagMatchingRuleBuilder RequireTagStructure(TagStructure tagStructure)
        {
            _tagStructure = tagStructure;

            return this;
        }

        public TagMatchingRuleBuilder RequireAttribute(RequiredAttributeDescriptor requiredAttributeDescriptor)
        {
            if (requiredAttributeDescriptor == null)
            {
                throw new ArgumentNullException(nameof(requiredAttributeDescriptor));
            }

            EnsureRequiredAttributeDescriptors();
            _requiredAttributeDescriptors.Add(requiredAttributeDescriptor);

            return this;
        }

        public TagMatchingRuleBuilder RequireAttribute(Action<RequiredAttributeDescriptorBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = RequiredAttributeDescriptorBuilder.Create();

            configure(builder);

            var requiredAttributeDescriptor = builder.Build();

            return RequireAttribute(requiredAttributeDescriptor);
        }

        public TagMatchingRuleBuilder AddDiagnostic(RazorDiagnostic diagnostic)
        {
            EnsureDiagnostics();
            _diagnostics.Add(diagnostic);

            return this;
        }

        public TagMatchingRule Build()
        {
            var validationDiagnostics = Validate();
            var diagnostics = new HashSet<RazorDiagnostic>(validationDiagnostics);
            if (_diagnostics != null)
            {
                diagnostics.UnionWith(_diagnostics);
            }

            var rule = new DefaultTagMatchingRule(
                _tagName,
                _parentTag,
                _tagStructure,
                _requiredAttributeDescriptors ?? Enumerable.Empty<RequiredAttributeDescriptor>(),
                diagnostics);

            return rule;
        }

        public void Reset()
        {
            _tagName = null;
            _parentTag = null;
            _tagStructure = default(TagStructure);
            _requiredAttributeDescriptors?.Clear();
            _diagnostics?.Clear();
        }

        private IEnumerable<RazorDiagnostic> Validate()
        {
            if (string.IsNullOrWhiteSpace(_tagName))
            {
                var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedTagNameNullOrWhitespace();

                yield return diagnostic;
            }
            else if (_tagName != TagHelperDescriptorProvider.ElementCatchAllTarget)
            {
                foreach (var character in _tagName)
                {
                    if (char.IsWhiteSpace(character) || InvalidNonWhitespaceTagNameCharacters.Contains(character))
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedTagName(_tagName, character);

                        yield return diagnostic;
                    }
                }
            }

            if (_parentTag != null)
            {
                if (string.IsNullOrWhiteSpace(_parentTag))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedParentTagNameNullOrWhitespace();

                    AddDiagnostic(diagnostic);
                }
                else
                {
                    foreach (var character in _parentTag)
                    {
                        if (char.IsWhiteSpace(character) || InvalidNonWhitespaceTagNameCharacters.Contains(character))
                        {
                            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidTargetedParentTagName(_parentTag, character);

                            AddDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }

        private void EnsureRequiredAttributeDescriptors()
        {
            if (_requiredAttributeDescriptors == null)
            {
                _requiredAttributeDescriptors = new HashSet<RequiredAttributeDescriptor>(RequiredAttributeDescriptorComparer.Default);
            }
        }

        private void EnsureDiagnostics()
        {
            if (_diagnostics == null)
            {
                _diagnostics = new HashSet<RazorDiagnostic>();
            }
        }

        private class DefaultTagMatchingRule : TagMatchingRule
        {
            public DefaultTagMatchingRule(
                string tagName,
                string parentTag,
                TagStructure tagStructure,
                IEnumerable<RequiredAttributeDescriptor> requiredAttributeDescriptors,
                IEnumerable<RazorDiagnostic> diagnostics)
            {
                TagName = tagName;
                ParentTag = parentTag;
                TagStructure = tagStructure;
                Attributes = new List<RequiredAttributeDescriptor>(requiredAttributeDescriptors);
                Diagnostics = new List<RazorDiagnostic>(diagnostics);
            }
        }
    }
}
