// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Internal;
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
        private Func<Task> _executeChildContentAsync;
        private Dictionary<HtmlEncoder, TagHelperContent> _perEncoderChildContent;
        private TagHelperAttributeList _allAttributes;

        /// <summary>
        /// Internal for testing purposes only.
        /// </summary>
        internal TagHelperExecutionContext(string tagName, TagMode tagMode)
            : this(tagName,
                   tagMode,
                   items: new Dictionary<object, object>(),
                   uniqueId: string.Empty,
                   executeChildContentAsync: () => TaskCache.CompletedTask,
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
            _allAttributes = new TagHelperAttributeList();

            Context = new TagHelperContext(_allAttributes, items, uniqueId);
            Output = new TagHelperOutput(tagName, new TagHelperAttributeList(), GetChildContentAsync)
            {
                TagMode = tagMode
            };

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

        // Internal set for testing.
        /// <summary>
        /// The <see cref="ITagHelper"/>'s output.
        /// </summary>
        public TagHelperOutput Output { get; internal set; }

        /// <summary>
        /// The <see cref="ITagHelper"/>'s context.
        /// </summary>
        public TagHelperContext Context { get; }

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
        /// Tracks the HTML attribute.
        /// </summary>
        /// <param name="name">The HTML attribute name.</param>
        /// <param name="value">The HTML attribute value.</param>
        /// <param name="valueStyle">The value style of the attribute.</param>
        public void AddHtmlAttribute(string name, object value, HtmlAttributeValueStyle valueStyle)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var attribute = new TagHelperAttribute(name, value, valueStyle);
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

            Output.Attributes.Add(attribute);
            _allAttributes.Add(attribute);
        }

        /// <summary>
        /// Tracks the <see cref="ITagHelper"/> bound attribute.
        /// </summary>
        /// <param name="name">The bound attribute name.</param>
        /// <param name="value">The attribute value.</param>
        /// <param name="valueStyle">The value style of the attribute.</param>
        public void AddTagHelperAttribute(string name, object value, HtmlAttributeValueStyle valueStyle)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var attribute = new TagHelperAttribute(name, value, valueStyle);
            _allAttributes.Add(attribute);
        }

        /// <summary>
        /// Tracks the <see cref="ITagHelper"/> bound attribute.
        /// </summary>
        /// <param name="attribute">The bound attribute.</param>
        public void AddTagHelperAttribute(TagHelperAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            _allAttributes.Add(attribute);
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

            Items = items;
            _executeChildContentAsync = executeChildContentAsync;
            _tagHelpers.Clear();
            _perEncoderChildContent?.Clear();
            _childContent = null;

            Context.Reinitialize(Items, uniqueId);
            Output.Reinitialize(tagName, tagMode);
        }

        /// <summary>
        /// Executes children asynchronously with the page's <see cref="HtmlEncoder" /> in scope and
        /// sets <see cref="Output"/>'s <see cref="TagHelperOutput.Content"/> to the rendered results.
        /// </summary>
        /// <returns>A <see cref="Task"/> that on completion sets <see cref="Output"/>'s
        /// <see cref="TagHelperOutput.Content"/> to the children's rendered content.</returns>
        public async Task SetOutputContentAsync()
        {
            var childContent = _childContent;

            if (childContent == null)
            {
                _startTagHelperWritingScope(null);
                await _executeChildContentAsync();
                childContent = _endTagHelperWritingScope();
            }

            Debug.Assert(!Output.IsContentModified);

            Output.Content.SetHtmlContent(childContent);
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
    }
}