// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

/// <summary>
/// <see cref="ITagHelper"/> implementation targeting &lt;environment&gt; elements that conditionally renders
/// content based on the current value of <see cref="IHostingEnvironment.EnvironmentName"/>.
/// If the environment is not listed in the specified <see cref="Names"/> or <see cref="Include"/>,
/// or if it is in <see cref="Exclude"/>, the content will not be rendered.
/// </summary>
public class EnvironmentTagHelper : TagHelper
{
    private static readonly char[] NameSeparator = new[] { ',' };

    /// <summary>
    /// Creates a new <see cref="EnvironmentTagHelper"/>.
    /// </summary>
    /// <param name="hostingEnvironment">The <see cref="IHostingEnvironment"/>.</param>
    public EnvironmentTagHelper(IWebHostEnvironment hostingEnvironment)
    {
        HostingEnvironment = hostingEnvironment;
    }

    /// <inheritdoc />
    public override int Order => -1000;

    /// <summary>
    /// A comma separated list of environment names in which the content should be rendered.
    /// If the current environment is also in the <see cref="Exclude"/> list, the content will not be rendered.
    /// </summary>
    /// <remarks>
    /// The specified environment names are compared case insensitively to the current value of
    /// <see cref="IHostingEnvironment.EnvironmentName"/>.
    /// </remarks>
    public string Names { get; set; }

    /// <summary>
    /// A comma separated list of environment names in which the content should be rendered.
    /// If the current environment is also in the <see cref="Exclude"/> list, the content will not be rendered.
    /// </summary>
    /// <remarks>
    /// The specified environment names are compared case insensitively to the current value of
    /// <see cref="IHostingEnvironment.EnvironmentName"/>.
    /// </remarks>
    public string Include { get; set; }

    /// <summary>
    /// A comma separated list of environment names in which the content will not be rendered.
    /// </summary>
    /// <remarks>
    /// The specified environment names are compared case insensitively to the current value of
    /// <see cref="IHostingEnvironment.EnvironmentName"/>.
    /// </remarks>
    public string Exclude { get; set; }

    /// <summary>
    /// Gets the <see cref="IWebHostEnvironment"/> for the application.
    /// </summary>
    protected IWebHostEnvironment HostingEnvironment { get; }

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(output);

        // Always strip the outer tag name as we never want <environment> to render
        output.TagName = null;

        if (string.IsNullOrWhiteSpace(Names) && string.IsNullOrWhiteSpace(Include) && string.IsNullOrWhiteSpace(Exclude))
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

        if (Exclude != null)
        {
            var tokenizer = new StringTokenizer(Exclude, NameSeparator);
            foreach (var item in tokenizer)
            {
                var environment = item.Trim();
                if (environment.HasValue && environment.Length > 0)
                {
                    if (environment.Equals(currentEnvironmentName, StringComparison.OrdinalIgnoreCase))
                    {
                        // Matching environment name found, suppress output
                        output.SuppressOutput();
                        return;
                    }
                }
            }
        }

        var hasEnvironments = false;
        if (Names != null)
        {
            var tokenizer = new StringTokenizer(Names, NameSeparator);
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
        }

        if (Include != null)
        {
            var tokenizer = new StringTokenizer(Include, NameSeparator);
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
        }

        if (hasEnvironments)
        {
            // This instance had at least one non-empty environment (names or include) specified but none of these
            // environments matched the current environment. Suppress the output in this case.
            output.SuppressOutput();
        }
    }
}
