// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Used to override <see cref="ITagHelper"/>'s behavior when its
    /// <see cref="ITagHelper.ProcessAsync"/> is invoked.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ContentBehaviorAttribute : Attribute
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="ContentBehaviorAttribute"/> class.
        /// </summary>
        /// <param name="contentBehavior">The <see cref="Razor.TagHelpers.ContentBehavior"/> for the
        /// <see cref="ITagHelper"/>.</param>
        public ContentBehaviorAttribute(ContentBehavior contentBehavior)
        {
            ContentBehavior = contentBehavior;
        }

        /// <summary>
        /// <see cref="Razor.TagHelpers.ContentBehavior"/> for the <see cref="ITagHelper"/>.
        /// </summary>
        public ContentBehavior ContentBehavior { get; private set; }
    }
}