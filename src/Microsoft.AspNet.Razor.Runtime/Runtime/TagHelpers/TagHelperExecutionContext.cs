// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Class used to store information about a <see cref="ITagHelper"/>'s execution lifetime.
    /// </summary>
    public class TagHelperExecutionContext
    {
        private readonly List<ITagHelper> _tagHelpers;
        private readonly Func<Task> _executeChildContentAsync;
        private readonly Action<HtmlEncoder> _startTagHelperWritingScope;
        private readonly Func<TagHelperContent> _endTagHelperWritingScope;
        private TagHelperContent _childContent;
        private Dictionary<HtmlEncoder, TagHelperContent> _perEncoderChildContent;

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

            if (startTagHelperWritingScope == null)
            {
                throw new ArgumentNullException(nameof(startTagHelperWritingScope));
            }

            if (endTagHelperWritingScope == null)
            {
                throw new ArgumentNullException(nameof(endTagHelperWritingScope));
            }

            _tagHelpers = new List<ITagHelper>();
            _executeChildContentAsync = executeChildContentAsync;
            _startTagHelperWritingScope = startTagHelperWritingScope;
            _endTagHelperWritingScope = endTagHelperWritingScope;

            TagMode = tagMode;
            HTMLAttributes = new TagHelperAttributeList();
            AllAttributes = new TagHelperAttributeList();
            TagName = tagName;
            Items = items;
            UniqueId = uniqueId;
        }

        /// <summary>
        /// Gets the HTML syntax of the element in the Razor source.
        /// </summary>
        public TagMode TagMode { get; }

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
        public IDictionary<object, object> Items { get; }

        /// <summary>
        /// HTML attributes.
        /// </summary>
        public TagHelperAttributeList HTMLAttributes { get; }

        /// <summary>
        /// <see cref="ITagHelper"/> bound attributes and HTML attributes.
        /// </summary>
        public TagHelperAttributeList AllAttributes { get; }

        /// <summary>
        /// An identifier unique to the HTML element this context is for.
        /// </summary>
        public string UniqueId { get; }

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
        /// The HTML tag name in the Razor source.
        /// </summary>
        public string TagName { get; }

        /// <summary>
        /// The <see cref="ITagHelper"/>s' output.
        /// </summary>
        public TagHelperOutput Output { get; set; }

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
        /// Tracks the minimized HTML attribute in <see cref="AllAttributes"/> and <see cref="HTMLAttributes"/>.
        /// </summary>
        /// <param name="name">The minimized HTML attribute name.</param>
        public void AddMinimizedHtmlAttribute(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            HTMLAttributes.Add(
                new TagHelperAttribute
                {
                    Name = name,
                    Minimized = true
                });
            AllAttributes.Add(
                new TagHelperAttribute
                {
                    Name = name,
                    Minimized = true
                });
        }

        /// <summary>
        /// Tracks the HTML attribute in <see cref="AllAttributes"/> and <see cref="HTMLAttributes"/>.
        /// </summary>
        /// <param name="name">The HTML attribute name.</param>
        /// <param name="value">The HTML attribute value.</param>
        public void AddHtmlAttribute(string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            HTMLAttributes.Add(name, value);
            AllAttributes.Add(name, value);
        }

        /// <summary>
        /// Tracks the <see cref="ITagHelper"/> bound attribute in <see cref="AllAttributes"/>.
        /// </summary>
        /// <param name="name">The bound attribute name.</param>
        /// <param name="value">The attribute value.</param>
        public void AddTagHelperAttribute(string name, object value)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            AllAttributes.Add(name, value);
        }

        /// <summary>
        /// Executes children asynchronously with the given <paramref name="encoder"/> in scope and returns their
        /// rendered content.
        /// </summary>
        /// <param name="useCachedResult">
        /// If <c>true</c>, multiple calls with the same <see cref="HtmlEncoder"/> will not cause children to
        /// re-execute; returns cached content.
        /// </param>
        /// <param name="encoder">
        /// The <see cref="HtmlEncoder"/> to use when the page handles
        /// non-<see cref="Microsoft.AspNet.Html.IHtmlContent"/> C# expressions. If <c>null</c>, executes children with
        /// the page's current <see cref="HtmlEncoder"/>.
        /// </param>
        /// <returns>A <see cref="Task"/> that on completion returns the rendered child content.</returns>
        public async Task<TagHelperContent> GetChildContentAsync(bool useCachedResult, HtmlEncoder encoder)
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

            return new DefaultTagHelperContent().SetContent(childContent);
        }
    }
}