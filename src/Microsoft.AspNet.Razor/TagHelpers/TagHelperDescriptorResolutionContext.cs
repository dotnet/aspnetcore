// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// Contains information needed to resolve <see cref="TagHelperDescriptor"/>s.
    /// </summary>
    public class TagHelperDescriptorResolutionContext
    {
        // Internal for testing purposes
        internal TagHelperDescriptorResolutionContext(IEnumerable<TagHelperDirectiveDescriptor> directiveDescriptors)
            : this(directiveDescriptors, new ErrorSink())
        {
        }

        /// <summary>
        /// Instantiates a new instance of <see cref="TagHelperDescriptorResolutionContext"/>.
        /// </summary>
        /// <param name="directiveDescriptors"><see cref="TagHelperDirectiveDescriptor"/>s used to resolve
        /// <see cref="TagHelperDescriptor"/>s.</param>
        /// <param name="errorSink">Used to aggregate <see cref="RazorError"/>s.</param>
        public TagHelperDescriptorResolutionContext(
            [NotNull] IEnumerable<TagHelperDirectiveDescriptor> directiveDescriptors,
            [NotNull] ErrorSink errorSink)
        {
            DirectiveDescriptors = new List<TagHelperDirectiveDescriptor>(directiveDescriptors);
            ErrorSink = errorSink;
        }

        /// <summary>
        /// <see cref="TagHelperDirectiveDescriptor"/>s used to resolve <see cref="TagHelperDescriptor"/>s.
        /// </summary>
        public IList<TagHelperDirectiveDescriptor> DirectiveDescriptors { get; private set; }

        /// <summary>
        /// Used to aggregate <see cref="RazorError"/>s.
        /// </summary>
        public ErrorSink ErrorSink { get; private set; }
    }
}