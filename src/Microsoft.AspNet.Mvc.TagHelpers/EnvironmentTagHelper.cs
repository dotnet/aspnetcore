// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNet.Mvc.TagHelpers
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
            // Always strip the outer tag name as we never want <environment> to render
            output.TagName = null;

            if (string.IsNullOrWhiteSpace(Names))
            {
                // No names specified, do nothing
                return;
            }

            var environments = Names.Split(NameSeparator, StringSplitOptions.RemoveEmptyEntries)
                                    .Where(name => !string.IsNullOrWhiteSpace(name));

            if (!environments.Any())
            {
                // Names contains only commas or empty entries, do nothing
                return;
            }

            var currentEnvironmentName = HostingEnvironment.EnvironmentName?.Trim();

            if (string.IsNullOrWhiteSpace(currentEnvironmentName))
            {
                // No current environment name, do nothing
                return;
            }

            if (environments.Any(name =>
                string.Equals(name.Trim(), currentEnvironmentName, StringComparison.OrdinalIgnoreCase)))
            {
                // Matching environment name found, do nothing
                return;
            }
            
            // No matching environment name found, suppress all output
            output.SuppressOutput();
        }
    }
}