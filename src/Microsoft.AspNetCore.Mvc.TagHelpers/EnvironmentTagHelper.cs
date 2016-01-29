// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// <see cref="ITagHelper"/> implementation targeting &lt;environment&gt; elements that conditionally renders
    /// content based on the current value of <see cref="IHostingEnvironment.EnvironmentName"/>.
    /// </summary>
    public class EnvironmentTagHelper : TagHelper
    {
        private static readonly char[] NameSeparator = new[] { ',' };

        /// <summary>
        /// Creates a new <see cref="EnvironmentTagHelper"/>.
        /// </summary>
        /// <param name="hostingEnvironment">The <see cref="IHostingEnvironment"/>.</param>
        public EnvironmentTagHelper(IHostingEnvironment hostingEnvironment)
        {
            HostingEnvironment = hostingEnvironment;
        }

        /// <inheritdoc />
        public override int Order => -1000;

        /// <summary>
        /// A comma separated list of environment names in which the content should be rendered.
        /// </summary>
        /// <remarks>
        /// The specified environment names are compared case insensitively to the current value of
        /// <see cref="IHostingEnvironment.EnvironmentName"/>.
        /// </remarks>
        public string Names { get; set; }

        protected IHostingEnvironment HostingEnvironment { get; }

        /// <inheritdoc />
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            // Always strip the outer tag name as we never want <environment> to render
            output.TagName = null;

            if (string.IsNullOrWhiteSpace(Names))
            {
                // No names specified, do nothing
                return;
            }

            var currentEnvironmentName = HostingEnvironment.EnvironmentName?.Trim();
            if (string.IsNullOrEmpty(currentEnvironmentName))
            {
                // No current environment name, do nothing
                return;
            }

            var tokenizer = new StringTokenizer(Names, NameSeparator);
            var hasEnvironments = false;
            foreach (var item in tokenizer)
            {
                var environment = item.Trim();
                if (environment.HasValue && environment.Length > 0)
                {
                    hasEnvironments = true;
                    if (environment.Equals(currentEnvironmentName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Matching environment name found, do nothing
                        return;
                    }
                }
            }

            if (hasEnvironments)
            {
                // This instance had at least one non-empty environment specified but none of these
                // environments matched the current environment. Suppress the output in this case.
                output.SuppressOutput();
            }
        }
    }
}