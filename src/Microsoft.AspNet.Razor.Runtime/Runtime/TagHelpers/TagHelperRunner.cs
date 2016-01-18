// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// A class used to run <see cref="ITagHelper"/>s.
    /// </summary>
    public class TagHelperRunner
    {
        /// <summary>
        /// Calls the <see cref="ITagHelper.ProcessAsync"/> method on <see cref="ITagHelper"/>s.
        /// </summary>
        /// <param name="executionContext">Contains information associated with running <see cref="ITagHelper"/>s.
        /// </param>
        /// <returns>Resulting <see cref="TagHelperOutput"/> from processing all of the
        /// <paramref name="executionContext"/>'s <see cref="ITagHelper"/>s.</returns>
        public async Task<TagHelperOutput> RunAsync(TagHelperExecutionContext executionContext)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            var tagHelperContext = new TagHelperContext(
                executionContext.AllAttributes,
                executionContext.Items,
                executionContext.UniqueId);

            OrderTagHelpers(executionContext.TagHelpers);

            for (var i = 0; i < executionContext.TagHelpers.Count; i++)
            {
                executionContext.TagHelpers[i].Init(tagHelperContext);
            }

            var tagHelperOutput = new TagHelperOutput(
                executionContext.TagName,
                executionContext.HTMLAttributes,
                executionContext.GetChildContentAsync)
            {
                TagMode = executionContext.TagMode,
            };

            for (var i = 0; i < executionContext.TagHelpers.Count; i++)
            {
                await executionContext.TagHelpers[i].ProcessAsync(tagHelperContext, tagHelperOutput);
            }

            return tagHelperOutput;
        }

        private static void OrderTagHelpers(IList<ITagHelper> tagHelpers)
        {
            // Using bubble-sort here due to its simplicity. It'd be an extreme corner case to ever have more than 3 or
            // 4 tag helpers simultaneously.
            ITagHelper temp = null;
            for (var i = 0; i < tagHelpers.Count; i++)
            {
                for (var j = i + 1; j < tagHelpers.Count; j++)
                {
                    if (tagHelpers[j].Order < tagHelpers[i].Order)
                    {
                        temp = tagHelpers[i];
                        tagHelpers[i] = tagHelpers[j];
                        tagHelpers[j] = temp;
                    }
                }
            }
        }
    }
}