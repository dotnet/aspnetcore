// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultBoundAttributeDescriptorBuilder : BoundAttributeDescriptorBuilder
    {
        private static readonly IReadOnlyDictionary<string, string> PrimitiveDisplayTypeNameLookups = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [typeof(byte).FullName] = "byte",
            [typeof(sbyte).FullName] = "sbyte",
            [typeof(int).FullName] = "int",
            [typeof(uint).FullName] = "uint",
            [typeof(short).FullName] = "short",
            [typeof(ushort).FullName] = "ushort",
            [typeof(long).FullName] = "long",
            [typeof(ulong).FullName] = "ulong",
            [typeof(float).FullName] = "float",
            [typeof(double).FullName] = "double",
            [typeof(char).FullName] = "char",
            [typeof(bool).FullName] = "bool",
            [typeof(object).FullName] = "object",
            [typeof(string).FullName] = "string",
            [typeof(decimal).FullName] = "decimal",
        };

        private readonly DefaultTagHelperDescriptorBuilder _parent;
        private readonly string _kind;

        private string _displayName;
        private bool _isEnum;
        private bool _hasIndexer;
        private string _indexerValueTypeName;
        private string _name;
        private string _typeName;
        private string _documentation;
        private string _indexerNamePrefix;
        private readonly Dictionary<string, string> _metadata;
        private HashSet<RazorDiagnostic> _diagnostics;

        public DefaultBoundAttributeDescriptorBuilder(DefaultTagHelperDescriptorBuilder parent, string kind)
        {
            _parent = parent;
            _kind = kind;

            _metadata = new Dictionary<string, string>();
        }

        public override BoundAttributeDescriptorBuilder Name(string name)
        {
            _name = name;

            return this;
        }

        public override BoundAttributeDescriptorBuilder PropertyName(string propertyName)
        {
            _metadata[TagHelperMetadata.Common.PropertyName] = propertyName;

            return this;
        }

        public override BoundAttributeDescriptorBuilder TypeName(string typeName)
        {
            _typeName = typeName;

            return this;
        }

        public override BoundAttributeDescriptorBuilder AsEnum()
        {
            _isEnum = true;

            return this;
        }

        public override BoundAttributeDescriptorBuilder AsDictionary(string attributeNamePrefix, string valueTypeName)
        {
            _indexerNamePrefix = attributeNamePrefix;
            _indexerValueTypeName = valueTypeName;
            _hasIndexer = true;

            return this;
        }

        public override BoundAttributeDescriptorBuilder Documentation(string documentation)
        {
            _documentation = documentation;

            return this;
        }

        public override BoundAttributeDescriptorBuilder AddMetadata(string key, string value)
        {
            _metadata[key] = value;

            return this;
        }

        public override BoundAttributeDescriptorBuilder AddDiagnostic(RazorDiagnostic diagnostic)
        {
            EnsureDiagnostics();
            _diagnostics.Add(diagnostic);

            return this;
        }

        public override BoundAttributeDescriptorBuilder DisplayName(string displayName)
        {
            if (displayName == null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            _displayName = displayName;

            return this;
        }

        public BoundAttributeDescriptor Build()
        {
            var validationDiagnostics = Validate();
            var diagnostics = new HashSet<RazorDiagnostic>(validationDiagnostics);
            if (_diagnostics != null)
            {
                diagnostics.UnionWith(_diagnostics);
            }

            var descriptor = new DefaultBoundAttributeDescriptor(
                _kind,
                _name,
                _typeName,
                _isEnum,
                _hasIndexer,
                _indexerNamePrefix,
                _indexerValueTypeName,
                _documentation,
                GetDisplayName(),
                new Dictionary<string, string>(_metadata),
                diagnostics.ToArray());

            return descriptor;
        }

        private string GetDisplayName()
        {
            if (_displayName != null)
            {
                return _displayName;
            }

            if (_typeName != null &&
                _metadata.ContainsKey(TagHelperMetadata.Common.PropertyName) &&
                _parent.Metadata.ContainsKey(TagHelperMetadata.Common.TypeName))
            {
                // This looks like a normal c# property, so lets compute a display name based on that.
                if (!PrimitiveDisplayTypeNameLookups.TryGetValue(_typeName, out var simpleTypeName))
                {
                    simpleTypeName = _typeName;
                }

                return $"{simpleTypeName} {_parent.Metadata[TagHelperMetadata.Common.TypeName]}.{_metadata[TagHelperMetadata.Common.PropertyName]}";
            }

            return _name;
        }

        private IEnumerable<RazorDiagnostic> Validate()
        {
            // data-* attributes are explicitly not implemented by user agents and are not intended for use on
            // the server; therefore it's invalid for TagHelpers to bind to them.
            const string DataDashPrefix = "data-";

            if (string.IsNullOrWhiteSpace(_name))
            {
                if (_indexerNamePrefix == null)
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeNullOrWhitespace(
                        _parent.GetDisplayName(),
                        GetDisplayName());

                    yield return diagnostic;
                }
            }
            else
            {
                if (_name.StartsWith(DataDashPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeNameStartsWith(
                        _parent.GetDisplayName(),
                        GetDisplayName(),
                        _name);

                    yield return diagnostic;
                }

                foreach (var character in _name)
                {
                    if (char.IsWhiteSpace(character) || HtmlConventions.InvalidNonWhitespaceHtmlCharacters.Contains(character))
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeName(
                            _parent.GetDisplayName(),
                            GetDisplayName(),
                            _name,
                            character);

                        yield return diagnostic;
                    }
                }
            }

            if (_indexerNamePrefix != null)
            {
                if (_indexerNamePrefix.StartsWith(DataDashPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributePrefixStartsWith(
                        _parent.GetDisplayName(),
                        GetDisplayName(),
                        _indexerNamePrefix);

                    yield return diagnostic;
                }
                else if (_indexerNamePrefix.Length > 0 && string.IsNullOrWhiteSpace(_indexerNamePrefix))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributeNullOrWhitespace(
                        _parent.GetDisplayName(),
                        GetDisplayName());

                    yield return diagnostic;
                }
                else
                {
                    foreach (var character in _indexerNamePrefix)
                    {
                        if (char.IsWhiteSpace(character) || HtmlConventions.InvalidNonWhitespaceHtmlCharacters.Contains(character))
                        {
                            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidBoundAttributePrefix(
                                _parent.GetDisplayName(),
                                GetDisplayName(),
                                _indexerNamePrefix,
                                character);

                            yield return diagnostic;
                        }
                    }
                }
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
