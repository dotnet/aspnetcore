// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <inheritdoc />
    public class TagHelperInitializer<TTagHelper> : ITagHelperInitializer<TTagHelper>
        where TTagHelper : ITagHelper
    {
        private readonly Action<TTagHelper, ViewContext> _initializeDelegate;

        /// <summary>
        /// Creates a <see cref="TagHelperInitializer{TTagHelper}"/>.
        /// </summary>
        /// <param name="action">The initialization delegate.</param>
        public TagHelperInitializer([NotNull] Action<TTagHelper, ViewContext> action)
        {
            _initializeDelegate = action;
        }

        /// <inheritdoc />
        public void Initialize([NotNull] TTagHelper helper, [NotNull] ViewContext context)
        {
            _initializeDelegate(helper, context);
        }
    }
}