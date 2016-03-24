// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Class used to store information about a <see cref="ITagHelper"/>'s execution lifetime.
    /// </summary>
    public class TagHelperExecutionContext
    {
        private readonly List<ITagHelper> _tagHelpers;
        private readonly Action<HtmlEncoder> _startTagHelperWritingScope;
        private readonly Func<TagHelperContent> _endTagHelperWritingScope;
        private TagHelperContent _childContent;
        private string _tagName;
        private string _uniqueId;
        private TagMode _tagMode;
        private Func<Task> _executeChildContentAsync;
        private Dictionary<HtmlEncoder, TagHelperContent> _perEncoderChildContent;
        private TagHelperAttributeList _htmlAttributes;
        private TagHelperAttributeList _allAttributes;

        /// <summary>
        /// Internal for testing purposes only.
        /// </summary>
        internal TagHelperExecutionContext(string tagName, TagMode tagMode)
            : this(tagName,
                   tagMode,
                   items: new Dictionary<object, object>(),
                   uniqueId: string.Empty,
                   executeChildContentAsync: async () => await Task.FromResult(result: true),
                   startTagHelperWritingScope: _ => { },
                   endTagHelperWritingScope: () => new DefaultTagHelperContent())
        {
        }

        /// <summary>
        /// Instantiates a new <see cref="TagHelperExecutionContext"/>.
        /// </summary>
        /// <param name="tagName">The HTML tag name in the Razor source.</param>
        /// <param name="tagMode">HTML syntax of the element in the Razor source.</param>
        /// <param name="items">The collection of items used to communicate with other
        /// <see cref="ITagHelper"/>s</param>
        /// <param name="uniqueId">An identifier unique to the HTML element this context is for.</param>
        /// <param name="executeChildContentAsync">A delegate used to execute the child content asynchronously.</param>
        /// <param name="startTagHelperWritingScope">
        /// A delegate used to start a writing scope in a Razor page and optionally override the page's
        /// <see cref="HtmlEncoder"/> within that scope.
        /// </param>
        /// <param name="endTagHelperWritingScope">A delegate used to end a writing scope in a Razor page.</param>
        public TagHelperExecutionContext(
            string tagName,
            TagMode tagMode,
            IDictionary<object, object> items,
            string uniqueId,
            Func<Task> executeChildContentAsync,
            Action<HtmlEncoder> startTagHelperWritingScope,
            Func<TagHelperContent> endTagHelperWritingScope)
        {
            if (startTagHelperWritingScope == null)
            {
                throw new ArgumentNullException(nameof(startTagHelperWritingScope));
            }

            if (endTagHelperWritingScope == null)
            {
                throw new ArgumentNullException(nameof(endTagHelperWritingScope));
            }

            _tagHelpers = new List<ITagHelper>();

            Reinitialize(tagName, tagMode, items, uniqueId, executeChildContentAsync);

            _startTagHelperWritingScope = startTagHelperWritingScope;
            _endTagHelperWritingScope = endTagHelperWritingScope;
        }

        /// <summary>
        /// Indicates if <see cref="GetChildContentAsync"/> has been called.
        /// </summary>
        public bool ChildContentRetrieved
        {
            get
            {
                return _childContent != null;
            }
        }

        /// <summary>
        /// Gets the collection of items used to communicate with other <see cref="ITagHelper"/>s.
        /// </summary>
        public IDictionary<object, object> Items { get; private set; }

        /// <summary>
        /// <see cref="ITagHelper"/>s that should be run.
        /// </summary>
        public IList<ITagHelper> TagHelpers
        {
            get
            {
                return _tagHelpers;
            }
        }

        /// <summary>
        /// The <see cref="ITagHelper"/>s' output.
        /// </summary>
        public TagHelperOutput Output { get; set; }

        public TagHelperContext CreateTagHelperContext() =>
            new TagHelperContext(_allAttributes, Items, _uniqueId);
        public TagHelperOutput CreateTagHelperOutput() =>
            new TagHelperOutput(_tagName, _htmlAttributes, GetChildContentAsync)
            {
                TagMode = _tagMode
            };

        /// <summary>
        /// Tracks the given <paramref name="tagHelper"/>.
        /// </summary>
        /// <param name="tagHelper">The tag helper to track.</param>
        public void Add(ITagHelper tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            _tagHelpers.Add(tagHelper);
        }

        /// <summary>
        /// Tracks the minimized HTML attribute.
        /// </summary>
        /// <param name="name">The minimized HTML attribute name.</param>
        public void AddMinimizedHtmlAttribute(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var attribute = new TagHelperAttribute(name);
            AddHtmlAttribute(attribute);
        }

        /// <summary>
        /// Tracks the HTML attribute.
        /// </summary>
        /// <param name="name">The HTML attribute name.</param>
        /// <param name="value">The HTML attribute value.</param>
        public void AddHtmlAttribute(string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var attribute = new TagHelperAttribute(name, value);
            AddHtmlAttribute(attribute);
        }

        /// <summary>
        /// Tracks the HTML attribute.
        /// </summary>
        /// <param name="attribute">The <see cref="TagHelperAttribute"/> to track.</param>
        public void AddHtmlAttribute(TagHelperAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            EnsureHtmlAttributes();
            EnsureAllAttributes();

            _htmlAttributes.Add(attribute);
            _allAttributes.Add(attribute);
        }

        /// <summary>
        /// Tracks the <see cref="ITagHelper"/> bound attribute.
        /// </summary>
        /// <param name="name">The bound attribute name.</param>
        /// <param name="value">The attribute value.</param>
        public void AddTagHelperAttribute(string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            EnsureAllAttributes();

            _allAttributes.Add(name, value);
        }

        /// <summary>
        /// Clears the <see cref="TagHelperExecutionContext"/> and updates its state with the provided values.
        /// </summary>
        /// <param name="tagName">The tag name to use.</param>
        /// <param name="tagMode">The <see cref="TagMode"/> to use.</param>
        /// <param name="items">The <see cref="IDictionary{Object, Object}"/> to use.</param>
        /// <param name="uniqueId">The unique id to use.</param>
        /// <param name="executeChildContentAsync">The <see cref="Func{Task}"/> to use.</param>
        public void Reinitialize(
            string tagName,
            TagMode tagMode,
            IDictionary<object, object> items,
            string uniqueId,
            Func<Task> executeChildContentAsync)
        {
            if (tagName == null)
            {
                throw new ArgumentNullException(nameof(tagName));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (uniqueId == null)
            {
                throw new ArgumentNullException(nameof(uniqueId));
            }

            if (executeChildContentAsync == null)
            {
                throw new ArgumentNullException(nameof(executeChildContentAsync));
            }

            _tagName = tagName;
            _tagMode = tagMode;
            Items = items;
            _uniqueId = uniqueId;
            _executeChildContentAsync = executeChildContentAsync;
            _tagHelpers.Clear();
            _perEncoderChildContent?.Clear();
            _htmlAttributes = null;
            _allAttributes = null;
            _childContent = null;
        }

        // Internal for testing.
        internal async Task<TagHelperContent> GetChildContentAsync(bool useCachedResult, HtmlEncoder encoder)
        {
            // Get cached content for this encoder.
            TagHelperContent childContent;
            if (encoder == null)
            {
                childContent = _childContent;
            }
            else
            {
                if (_perEncoderChildContent == null)
                {
                    childContent = null;
                    _perEncoderChildContent = new Dictionary<HtmlEncoder, TagHelperContent>();
                }
                else
                {
                    _perEncoderChildContent.TryGetValue(encoder, out childContent);
                }
            }

            if (!useCachedResult || childContent == null)
            {
                _startTagHelperWritingScope(encoder);
                await _executeChildContentAsync();
                childContent = _endTagHelperWritingScope();

                if (encoder == null)
                {
                    _childContent = childContent;
                }
                else
                {
                    _perEncoderChildContent[encoder] = childContent;
                }
            }

            return new DefaultTagHelperContent().SetHtmlContent(childContent);
        }

        private void EnsureHtmlAttributes()
        {
            if (_htmlAttributes == null)
            {
                _htmlAttributes = new TagHelperAttributeList();
            }
        }

        private void EnsureAllAttributes()
        {
            if (_allAttributes == null)
            {
                _allAttributes = new TagHelperAttributeList();
            }
        }
    }
}