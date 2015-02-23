// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Abstract class used to buffer content returned by <see cref="ITagHelper"/>s.
    /// </summary>
    public abstract class TagHelperContent : IEnumerable<string>
    {
        /// <summary>
        /// Gets a value indicating whether the content was modifed.
        /// </summary>
        public abstract bool IsModified { get; }

        /// <summary>
        /// Gets a value indicating whether the content is empty.
        /// </summary>
        public abstract bool IsEmpty { get; }

        /// <summary>
        /// Gets a value indicating whether the content is whitespace.
        /// </summary>
        public abstract bool IsWhiteSpace { get; }

        /// <summary>
        /// Sets the content.
        /// </summary>
        /// <param name="value">The <see cref="string"/> that replaces the content.</param>
        /// <returns>A reference to this instance after the set operation has completed.</returns>
        public abstract TagHelperContent SetContent(string value);

        /// <summary>
        /// Sets the content.
        /// </summary>
        /// <param name="tagHelperContent">The <see cref="TagHelperContent"/> that replaces the content.</param>
        /// <returns>A reference to this instance after the set operation has completed.</returns>
        public abstract TagHelperContent SetContent(TagHelperContent tagHelperContent);

        /// <summary>
        /// Appends <paramref name="value"/> to the existing content.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to be appended.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public abstract TagHelperContent Append(string value);

        /// <summary>
        /// Appends <paramref name="tagHelperContent"/> to the existing content.
        /// </summary>
        /// <param name="tagHelperContent">The <see cref="TagHelperContent"/> to be appended.</param>
        /// <returns>A reference to this instance after the append operation has completed.</returns>
        public abstract TagHelperContent Append(TagHelperContent tagHelperContent);

        /// <summary>
        /// Clears the content.
        /// </summary>
        /// <returns>A reference to this instance after the clear operation has completed.</returns>
        public abstract TagHelperContent Clear();

        /// <summary>
        /// Gets the content.
        /// </summary>
        /// <returns>A <see cref="string"/> containing the content.</returns>
        public abstract string GetContent();

        /// <inheritdoc />
        public abstract IEnumerator<string> GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}