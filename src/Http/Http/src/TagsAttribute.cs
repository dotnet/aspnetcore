// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Specifies a collection of tags in <see cref="Endpoint.Metadata"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Delegate | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class TagsAttribute : Attribute, ITagsMetadata
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="tags"></param>
        public TagsAttribute(params string[] tags)
        {
            Tags = new List<string>(tags);
        }

        /// <summary>
        /// Gets the collection of tags associated with the endpoint.
        /// </summary>
        public IList<string> Tags { get; }
    }
}
