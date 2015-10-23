// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// Class used to represent the output of an <see cref="ITagHelper"/>.
    /// </summary>
    public class TagHelperOutput
    {
        private readonly Func<bool, Task<TagHelperContent>> _getChildContentAsync;

        // Internal for testing
        internal TagHelperOutput(string tagName)
            : this(
                tagName,
                new TagHelperAttributeList(),
                (cachedResult) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()))
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperOutput"/>.
        /// </summary>
        /// <param name="tagName">The HTML element's tag name.</param>
        /// <param name="attributes">The HTML attributes.</param>
        /// <param name="getChildContentAsync">A delegate used to execute and retrieve the rendered child content
        /// asynchronously.</param>
        public TagHelperOutput(
            string tagName,
            TagHelperAttributeList attributes,
            Func<bool, Task<TagHelperContent>> getChildContentAsync)
        {
            if (attributes == null)
            {
                throw new ArgumentNullException(nameof(attributes));
            }

            if (getChildContentAsync == null)
            {
                throw new ArgumentNullException(nameof(getChildContentAsync));
            }

            TagName = tagName;
            Attributes = new TagHelperAttributeList(attributes);
            _getChildContentAsync = getChildContentAsync;
        }

        /// <summary>
        /// The HTML element's tag name.
        /// </summary>
        /// <remarks>
        /// A whitespace or <c>null</c> value results in no start or end tag being rendered.
        /// </remarks>
        public string TagName { get; set; }

        /// <summary>
        /// Content that precedes the HTML element.
        /// </summary>
        /// <remarks>Value is rendered before the HTML element.</remarks>
        public TagHelperContent PreElement { get; } = new DefaultTagHelperContent();

        /// <summary>
        /// The HTML element's pre content.
        /// </summary>
        /// <remarks>Value is prepended to the <see cref="ITagHelper"/>'s final output.</remarks>
        public TagHelperContent PreContent { get; } = new DefaultTagHelperContent();

        /// <summary>
        /// The HTML element's main content.
        /// </summary>
        /// <remarks>Value occurs in the <see cref="ITagHelper"/>'s final output after <see cref="PreContent"/> and
        /// before <see cref="PostContent"/></remarks>
        public TagHelperContent Content { get; } = new DefaultTagHelperContent();

        /// <summary>
        /// The HTML element's post content.
        /// </summary>
        /// <remarks>Value is appended to the <see cref="ITagHelper"/>'s final output.</remarks>
        public TagHelperContent PostContent { get; } = new DefaultTagHelperContent();

        /// <summary>
        /// Content that follows the HTML element.
        /// </summary>
        /// <remarks>Value is rendered after the HTML element.</remarks>
        public TagHelperContent PostElement { get; } = new DefaultTagHelperContent();

        /// <summary>
        /// <c>true</c> if <see cref="Content"/> has been set, <c>false</c> otherwise.
        /// </summary>
        public bool IsContentModified
        {
            get
            {
                return Content.IsModified;
            }
        }

        /// <summary>
        /// Syntax of the element in the generated HTML.
        /// </summary>
        public TagMode TagMode { get; set; }

        /// <summary>
        /// The HTML element's attributes.
        /// </summary>
        /// <remarks>
        /// MVC will HTML encode <see cref="string"/> values when generating the start tag. It will not HTML encode
        /// a <c>Microsoft.AspNet.Mvc.Rendering.HtmlString</c> instance. MVC converts most other types to a
        /// <see cref="string"/>, then HTML encodes the result.
        /// </remarks>
        public TagHelperAttributeList Attributes { get; }

        /// <summary>
        /// Changes <see cref="TagHelperOutput"/> to generate nothing.
        /// </summary>
        /// <remarks>
        /// Sets <see cref="TagName"/> to <c>null</c>, and clears <see cref="PreElement"/>, <see cref="PreContent"/>,
        /// <see cref="Content"/>, <see cref="PostContent"/>, and <see cref="PostElement"/> to suppress output.
        /// </remarks>
        public void SuppressOutput()
        {
            TagName = null;
            PreElement.Clear();
            PreContent.Clear();
            Content.Clear();
            PostContent.Clear();
            PostElement.Clear();
        }

        /// <summary>
        /// A delegate used to execute children asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> that on completion returns content rendered by children.</returns>
        /// <remarks>This method is memoized.</remarks>
        public Task<TagHelperContent> GetChildContentAsync()
        {
            return GetChildContentAsync(useCachedResult: true);
        }

        /// <summary>
        /// A delegate used to execute children asynchronously.
        /// </summary>
        /// <param name="useCachedResult">If <c>true</c> multiple calls to this method will not cause re-execution
        /// of child content; cached content will be returned.</param>
        /// <returns>A <see cref="Task"/> that on completion returns content rendered by children.</returns>
        public Task<TagHelperContent> GetChildContentAsync(bool useCachedResult)
        {
            return _getChildContentAsync(useCachedResult);
        }
    }
}