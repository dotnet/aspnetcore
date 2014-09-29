// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Used to override a <see cref="ITagHelper"/>'s default tag name target.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class TagNameAttribute : Attribute
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="TagNameAttribute"/> class.
        /// </summary>
        /// <param name="tag">The HTML tag name for the <see cref="TagHelper"/> to target.</param>
        public TagNameAttribute([NotNull] string tag)
        {
            Tags = new[] { tag };
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="TagNameAttribute"/> class.
        /// </summary>
        /// <param name="tag">The HTML tag name for the <see cref="TagHelper"/> to target.</param>
        /// <param name="additionalTags">Additional HTML tag names for the <see cref="TagHelper"/> to target.</param>
        public TagNameAttribute([NotNull] string tag, [NotNull] params string[] additionalTags)
        {
            if (additionalTags.Contains(null))
            {
                throw new ArgumentNullException(
                    nameof(additionalTags),
                    Resources.FormatTagNameAttribute_AdditionalTagsCannotContainNull(nameof(additionalTags)));
            };

            var allTags = new List<string>(additionalTags);
            allTags.Add(tag);

            Tags = allTags;
        }

        /// <summary>
        /// An <see cref="IEnumerable{string}"/> of tag names for the <see cref="TagHelper"/> to target.
        /// </summary>
        public IEnumerable<string> Tags { get; private set; }
    }
}