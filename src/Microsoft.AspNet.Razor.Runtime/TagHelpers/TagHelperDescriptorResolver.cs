// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Used to resolve <see cref="TagHelperDescriptor"/>s.
    /// </summary>
    public class TagHelperDescriptorResolver : ITagHelperDescriptorResolver
    {
        private static readonly IReadOnlyDictionary<TagHelperDirectiveType, string> _directiveNames =
            new Dictionary<TagHelperDirectiveType, string>
            {
                { TagHelperDirectiveType.AddTagHelper, SyntaxConstants.CSharp.AddTagHelperKeyword },
                { TagHelperDirectiveType.RemoveTagHelper, SyntaxConstants.CSharp.RemoveTagHelperKeyword },
                { TagHelperDirectiveType.TagHelperPrefix, SyntaxConstants.CSharp.TagHelperPrefixKeyword },
            };

        private readonly TagHelperTypeResolver _typeResolver;
        private readonly bool _designTime;

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagHelperDescriptorResolver"/> class.
        /// </summary>
        /// <param name="designTime">Indicates whether resolved <see cref="TagHelperDescriptor"/>s should include
        /// design time specific information.</param>
        public TagHelperDescriptorResolver(bool designTime)
            : this(new TagHelperTypeResolver(), designTime)
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperDescriptorResolver"/> class with the
        /// specified <paramref name="typeResolver"/>.
        /// </summary>
        /// <param name="typeResolver">The <see cref="TagHelperTypeResolver"/>.</param>
        /// <param name="designTime">Indicates whether resolved <see cref="TagHelperDescriptor"/>s should include
        /// design time specific information.</param>
        public TagHelperDescriptorResolver(TagHelperTypeResolver typeResolver, bool designTime)
        {
            _typeResolver = typeResolver;
            _designTime = designTime;
        }

        /// <inheritdoc />
        public IEnumerable<TagHelperDescriptor> Resolve([NotNull] TagHelperDescriptorResolutionContext context)
        {
            var resolvedDescriptors = new HashSet<TagHelperDescriptor>(TagHelperDescriptorComparer.Default);

            // tagHelperPrefix directives do not affect which TagHelperDescriptors are added or removed from the final
            // list, need to remove them.
            var actionableDirectiveDescriptors = context.DirectiveDescriptors.Where(
                directive => directive.DirectiveType != TagHelperDirectiveType.TagHelperPrefix);

            foreach (var directiveDescriptor in actionableDirectiveDescriptors)
            {
                try
                {
                    var lookupInfo = GetLookupInfo(directiveDescriptor, context.ErrorSink);

                    // Could not resolve the lookup info.
                    if (lookupInfo == null)
                    {
                        return Enumerable.Empty<TagHelperDescriptor>();
                    }

                    if (directiveDescriptor.DirectiveType == TagHelperDirectiveType.RemoveTagHelper)
                    {
                        resolvedDescriptors.RemoveWhere(descriptor => MatchesLookupInfo(descriptor, lookupInfo));
                    }
                    else if (directiveDescriptor.DirectiveType == TagHelperDirectiveType.AddTagHelper)
                    {
                        var descriptors = ResolveDescriptorsInAssembly(
                            lookupInfo.AssemblyName,
                            directiveDescriptor.Location,
                            context.ErrorSink);

                        // Only use descriptors that match our lookup info
                        descriptors = descriptors.Where(descriptor => MatchesLookupInfo(descriptor, lookupInfo));

                        resolvedDescriptors.UnionWith(descriptors);
                    }
                }
                catch (Exception ex)
                {
                    string directiveName;
                    _directiveNames.TryGetValue(directiveDescriptor.DirectiveType, out directiveName);
                    Debug.Assert(!string.IsNullOrEmpty(directiveName));

                    context.ErrorSink.OnError(
                        directiveDescriptor.Location,
                        Resources.FormatTagHelperDescriptorResolver_EncounteredUnexpectedError(
                            "@" + directiveName,
                            directiveDescriptor.DirectiveText,
                            ex.Message));
                }
            }

            var prefixedDescriptors = PrefixDescriptors(context, resolvedDescriptors);

            return prefixedDescriptors;
        }

        /// <summary>
        /// Resolves all <see cref="TagHelperDescriptor"/>s for <see cref="ITagHelper"/>s from the given
        /// <paramref name="assemblyName"/>.
        /// </summary>
        /// <param name="assemblyName">
        /// The name of the assembly to resolve <see cref="TagHelperDescriptor"/>s from.
        /// </param>
        /// <param name="documentLocation">The <see cref="SourceLocation"/> of the directive.</param>
        /// <param name="errorSink">Used to record errors found when resolving <see cref="TagHelperDescriptor"/>s
        /// within the given <paramref name="assemblyName"/>.</param>
        /// <returns><see cref="TagHelperDescriptor"/>s for <see cref="ITagHelper"/>s from the given
        /// <paramref name="assemblyName"/>.</returns>
        // This is meant to be overridden by tooling to enable assembly level caching.
        protected virtual IEnumerable<TagHelperDescriptor> ResolveDescriptorsInAssembly(
            string assemblyName,
            SourceLocation documentLocation,
            ErrorSink errorSink)
        {
            // Resolve valid tag helper types from the assembly.
            var tagHelperTypes = _typeResolver.Resolve(assemblyName, documentLocation, errorSink);

            // Convert types to TagHelperDescriptors
            var descriptors = tagHelperTypes.SelectMany(
                type => TagHelperDescriptorFactory.CreateDescriptors(assemblyName, type, _designTime, errorSink));

            return descriptors;
        }

        private static IEnumerable<TagHelperDescriptor> PrefixDescriptors(
            TagHelperDescriptorResolutionContext context,
            IEnumerable<TagHelperDescriptor> descriptors)
        {
            var tagHelperPrefix = ResolveTagHelperPrefix(context);

            if (!string.IsNullOrEmpty(tagHelperPrefix))
            {
                return descriptors.Select(descriptor =>
                    new TagHelperDescriptor(
                        tagHelperPrefix,
                        descriptor.TagName,
                        descriptor.TypeName,
                        descriptor.AssemblyName,
                        descriptor.Attributes,
                        descriptor.RequiredAttributes,
                        descriptor.DesignTimeDescriptor));
            }

            return descriptors;
        }

        private static string ResolveTagHelperPrefix(TagHelperDescriptorResolutionContext context)
        {
            var prefixDirectiveDescriptors = context.DirectiveDescriptors.Where(
                descriptor => descriptor.DirectiveType == TagHelperDirectiveType.TagHelperPrefix);

            TagHelperDirectiveDescriptor prefixDirective = null;

            foreach (var directive in prefixDirectiveDescriptors)
            {
                if (prefixDirective == null)
                {
                    prefixDirective = directive;
                }
                else
                {
                    // For each invalid @tagHelperPrefix we need to create an error.
                    context.ErrorSink.OnError(
                        directive.Location,
                        Resources.FormatTagHelperDescriptorResolver_InvalidTagHelperDirective(
                            SyntaxConstants.CSharp.TagHelperPrefixKeyword));
                }
            }

            var prefix = prefixDirective?.DirectiveText;

            if (prefix != null && !EnsureValidPrefix(prefix, prefixDirective.Location, context.ErrorSink))
            {
                prefix = null;
            }

            return prefix;
        }

        private static bool EnsureValidPrefix(
            string prefix,
            SourceLocation directiveLocation,
            ErrorSink errorSink)
        {
            foreach (var character in prefix)
            {
                // Prefixes are correlated with tag names, tag names cannot have whitespace.
                if (char.IsWhiteSpace(character) ||
                    TagHelperDescriptorFactory.InvalidNonWhitespaceNameCharacters.Contains(character))
                {
                    errorSink.OnError(
                        directiveLocation,
                        Resources.FormatTagHelperDescriptorResolver_InvalidTagHelperPrefixValue(
                            SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                            character,
                            prefix));

                    return false;
                }
            }

            return true;
        }

        private static bool MatchesLookupInfo(TagHelperDescriptor descriptor, LookupInfo lookupInfo)
        {
            if (!string.Equals(descriptor.AssemblyName, lookupInfo.AssemblyName, StringComparison.Ordinal))
            {
                return false;
            }

            // We need to escape the TypePattern so we can choose to only allow specific regex.
            var escaped = Regex.Escape(lookupInfo.TypePattern);

            // We surround the escaped with ^ and $ in order ot ensure a regex match matches the entire
            // string. We also replace any '*' or '?' characters with regex to match appropriate content.
            // '*' matches 0 or more characters lazily and '?' matches 1 character.
            var pattern = "^" + escaped.Replace(@"\?", ".").Replace(@"\*", ".*?") + "$";
            var regex = new Regex(pattern, RegexOptions.Singleline);

            return regex.IsMatch(descriptor.TypeName);
        }

        private static LookupInfo GetLookupInfo(TagHelperDirectiveDescriptor directiveDescriptor,
                                                ErrorSink errorSink)
        {
            var lookupText = directiveDescriptor.DirectiveText;
            var lookupStrings = lookupText?.Split(new[] { ',' });

            // Ensure that we have valid lookupStrings to work with. Valid formats are:
            // "assemblyName"
            // "typeName, assemblyName"
            if (lookupStrings == null ||
                lookupStrings.Any(string.IsNullOrWhiteSpace) ||
                lookupStrings.Length != 2)
            {
                errorSink.OnError(
                    directiveDescriptor.Location,
                    Resources.FormatTagHelperDescriptorResolver_InvalidTagHelperLookupText(lookupText));

                return null;
            }

            return new LookupInfo
            {
                TypePattern = lookupStrings[0].Trim(),
                AssemblyName = lookupStrings[1].Trim()
            };
        }

        private class LookupInfo
        {
            public string AssemblyName { get; set; }

            public string TypePattern { get; set; }
        }
    }
}