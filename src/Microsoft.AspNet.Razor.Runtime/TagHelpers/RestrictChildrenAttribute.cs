// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Restricts children of the <see cref="ITagHelper"/>'s element.
    /// </summary>
    /// <remarks>Combining this attribute with a <see cref="TargetElementAttribute"/> that specifies its
    /// <see cref="TargetElementAttribute.TagStructure"/> as <see cref="TagStructure.WithoutEndTag"/> will result in
    /// this attribute being ignored.</remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class RestrictChildrenAttribute : Attribute
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="RestrictChildrenAttribute"/> class.
        /// </summary>
        /// <param name="childTag">
        /// The tag name of an element allowed as a child. Tag helpers must target the element.
        /// </param>
        /// <param name="childTags">
        /// Additional names of elements allowed as children. Tag helpers must target all such elements.
        /// </param>
        public RestrictChildrenAttribute(string childTag, params string[] childTags)
        {
            var concatenatedNames = new string[1 + childTags.Length];
            concatenatedNames[0] = childTag;

            childTags.CopyTo(concatenatedNames, 1);

            ChildTags = concatenatedNames;
        }

        /// <summary>
        /// Get the names of elements allowed as children. Tag helpers must target all such elements.
        /// </summary>
        public IEnumerable<string> ChildTags { get; }
    }
}
