// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// A class used to run <see cref="ITagHelper"/>s.
    /// </summary>
    public class TagHelperRunner
    {
        /// <summary>
        /// Calls the <see cref="ITagHelperComponent.ProcessAsync"/> method on <see cref="ITagHelper"/>s.
        /// </summary>
        /// <param name="executionContext">Contains information associated with running <see cref="ITagHelper"/>s.
        /// </param>
        /// <returns>Resulting <see cref="TagHelperOutput"/> from processing all of the
        /// <paramref name="executionContext"/>'s <see cref="ITagHelper"/>s.</returns>
        public Task RunAsync(TagHelperExecutionContext executionContext)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            var tagHelperContext = executionContext.Context;
            var tagHelpers = executionContext.TagHelpers;
            OrderTagHelpers(tagHelpers);

            // Read interface .Count once rather than per iteration
            var count = tagHelpers.Count;
            for (var i = 0; i < count; i++)
            {
                tagHelpers[i].Init(tagHelperContext);
            }

            var tagHelperOutput = executionContext.Output;

            for (var i = 0; i < count; i++)
            {
                var task = tagHelpers[i].ProcessAsync(tagHelperContext, tagHelperOutput);
                if (!task.IsCompletedSuccessfully)
                {
                    return Awaited(task, executionContext, i + 1, count);
                }
            }

            return Task.CompletedTask;

            static async Task Awaited(Task task, TagHelperExecutionContext executionContext, int i, int count)
            {
                await task;

                var tagHelpers = executionContext.TagHelpers;
                var tagHelperOutput = executionContext.Output;
                var tagHelperContext = executionContext.Context;
                for (; i < count; i++)
                {
                    await tagHelpers[i].ProcessAsync(tagHelperContext, tagHelperOutput);
                }
            }
        }

        private static void OrderTagHelpers(IList<ITagHelper> tagHelpers)
        {
            // Using bubble-sort here due to its simplicity. It'd be an extreme corner case to ever have more than 3 or
            // 4 tag helpers simultaneously.

            // Read interface .Count once rather than per iteration
            var count = tagHelpers.Count;
            for (var i = 0; i < count; i++)
            {
                for (var j = i + 1; j < count; j++)
                {
                    if (tagHelpers[j].Order < tagHelpers[i].Order)
                    {
                        var temp = tagHelpers[i];
                        tagHelpers[i] = tagHelpers[j];
                        tagHelpers[j] = temp;
                    }
                }
            }
        }
    }
}