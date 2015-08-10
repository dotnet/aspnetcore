// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Class used to store information about a <see cref="ITagHelper"/>'s execution lifetime.
    /// </summary>
    public class TagHelperExecutionContext
    {
        private readonly List<ITagHelper> _tagHelpers;
        private readonly Func<Task> _executeChildContentAsync;
        private readonly Action _startTagHelperWritingScope;
        private readonly Func<TagHelperContent> _endTagHelperWritingScope;
        private TagHelperContent _childContent;

        /// <summary>
        /// Internal for testing purposes only.
        /// </summary>
        internal TagHelperExecutionContext(string tagName, TagMode tagMode)
            : this(tagName,
                   tagMode,
                   items: new Dictionary<object, object>(),
                   uniqueId: string.Empty,
                   executeChildContentAsync: async () => await Task.FromResult(result: true),
                   startTagHelperWritingScope: () => { },
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
        /// <param name="startTagHelperWritingScope">A delegate used to start a writing scope in a Razor page.</param>
        /// <param name="endTagHelperWritingScope">A delegate used to end a writing scope in a Razor page.</param>
        public TagHelperExecutionContext(
            [NotNull] string tagName,
            TagMode tagMode,
            [NotNull] IDictionary<object, object> items,
            [NotNull] string uniqueId,
            [NotNull] Func<Task> executeChildContentAsync,
            [NotNull] Action startTagHelperWritingScope,
            [NotNull] Func<TagHelperContent> endTagHelperWritingScope)
        {
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
        public IEnumerable<ITagHelper> TagHelpers
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
        public void Add([NotNull] ITagHelper tagHelper)
        {
            _tagHelpers.Add(tagHelper);
        }

        /// <summary>
        /// Tracks the minimized HTML attribute in <see cref="AllAttributes"/> and <see cref="HTMLAttributes"/>.
        /// </summary>
        /// <param name="name">The minimized HTML attribute name.</param>
        public void AddMinimizedHtmlAttribute([NotNull] string name)
        {
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
        public void AddHtmlAttribute([NotNull] string name, object value)
        {
            HTMLAttributes.Add(name, value);
            AllAttributes.Add(name, value);
        }

        /// <summary>
        /// Tracks the <see cref="ITagHelper"/> bound attribute in <see cref="AllAttributes"/>.
        /// </summary>
        /// <param name="name">The bound attribute name.</param>
        /// <param name="value">The attribute value.</param>
        public void AddTagHelperAttribute([NotNull] string name, object value)
        {
            AllAttributes.Add(name, value);
        }

        /// <summary>
        /// Executes the child content asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> which on completion executes all child content.</returns>
        public Task ExecuteChildContentAsync()
        {
            return _executeChildContentAsync();
        }

        /// <summary>
        /// Execute and retrieve the rendered child content asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> that on completion returns the rendered child content.</returns>
        /// <remarks>
        /// Child content is only executed once. Successive calls to this method or successive executions of the
        /// returned <see cref="Task{TagHelperContent}"/> return a cached result.
        /// </remarks>
        public async Task<TagHelperContent> GetChildContentAsync(bool useCachedResult)
        {
            if (!useCachedResult || _childContent == null)
            {
                _startTagHelperWritingScope();
                await _executeChildContentAsync();
                _childContent = _endTagHelperWritingScope();
            }

            return new DefaultTagHelperContent().SetContent(_childContent);
        }
    }
}