// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Contains information related to the execution of <see cref="ITagHelper"/>s.
    /// </summary>
    public class TagHelperContext
    {
        private readonly Func<bool, Task<TagHelperContent>> _getChildContentAsync;

        /// <summary>
        /// Instantiates a new <see cref="TagHelperContext"/>.
        /// </summary>
        /// <param name="allAttributes">Every attribute associated with the current HTML element.</param>
        /// <param name="items">Collection of items used to communicate with other <see cref="ITagHelper"/>s.</param>
        /// <param name="uniqueId">The unique identifier for the source element this <see cref="TagHelperContext" />
        /// applies to.</param>
        /// <param name="getChildContentAsync">A delegate used to execute and retrieve the rendered child content
        /// asynchronously.</param>
        public TagHelperContext(
            [NotNull] IEnumerable<IReadOnlyTagHelperAttribute> allAttributes,
            [NotNull] IDictionary<object, object> items,
            [NotNull] string uniqueId,
            [NotNull] Func<bool, Task<TagHelperContent>> getChildContentAsync)
        {
            AllAttributes = new ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute>(
                allAttributes.Select(attribute => new TagHelperAttribute(attribute.Name, attribute.Value)));
            Items = items;
            UniqueId = uniqueId;
            _getChildContentAsync = getChildContentAsync;
        }

        /// <summary>
        /// Every attribute associated with the current HTML element.
        /// </summary>
        public ReadOnlyTagHelperAttributeList<IReadOnlyTagHelperAttribute> AllAttributes { get; }

        /// <summary>
        /// Gets the collection of items used to communicate with other <see cref="ITagHelper"/>s.
        /// </summary>
        /// <remarks>
        /// This <see cref="IDictionary{object, object}"/> is copy-on-write in order to ensure items added to this
        /// collection are visible only to other <see cref="ITagHelper"/>s targeting child elements.
        /// </remarks>
        public IDictionary<object, object> Items { get; }

        /// <summary>
        /// An identifier unique to the HTML element this context is for.
        /// </summary>
        public string UniqueId { get; }

        /// <summary>
        /// A delegate used to execute and retrieve the rendered child content asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> that when executed returns content rendered by children.</returns>
        /// <remarks>This method is memoized.</remarks>
        public Task<TagHelperContent> GetChildContentAsync()
        {
            return GetChildContentAsync(useCachedResult: true);
        }

        /// <summary>
        /// A delegate used to execute and retrieve the rendered child content asynchronously.
        /// </summary>
        /// <param name="useCachedResult">If <c>true</c> multiple calls to this method will not cause re-execution
        /// of child content; cached content will be returned.</param>
        /// <returns>A <see cref="Task"/> that when executed returns content rendered by children.</returns>
        public Task<TagHelperContent> GetChildContentAsync(bool useCachedResult)
        {
            return _getChildContentAsync(useCachedResult);
        }
    }
}