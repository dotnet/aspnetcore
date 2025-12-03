// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// A component that renders its child content only when the current hosting environment
/// matches one of the specified environment names.
/// </summary>
/// <remarks>
/// <para>
/// This component is similar to the environment tag helper in MVC and Razor Pages.
/// </para>
/// <example>
/// The following example shows how to conditionally render content based on the environment:
/// <code>
/// &lt;EnvironmentBoundary Include="Development"&gt;
///     &lt;div class="alert alert-warning"&gt;
///         You are running in Development mode. Debug features are enabled.
///     &lt;/div&gt;
/// &lt;/EnvironmentBoundary&gt;
///
/// &lt;EnvironmentBoundary Include="Development, Staging"&gt;
///     &lt;p&gt;This is a pre-production environment.&lt;/p&gt;
/// &lt;/EnvironmentBoundary&gt;
///
/// &lt;EnvironmentBoundary Exclude="Production"&gt;
///     &lt;p&gt;Debug information: @DateTime.Now&lt;/p&gt;
/// &lt;/EnvironmentBoundary&gt;
/// </code>
/// </example>
/// </remarks>
public sealed class EnvironmentBoundary : ComponentBase
{
    private static readonly char[] NameSeparator = [','];

    [Inject]
    private IHostEnvironment HostEnvironment { get; set; } = default!;

    /// <summary>
    /// Gets or sets a comma-separated list of environment names in which the content should be rendered.
    /// If the current environment is also in the <see cref="Exclude"/> list, the content will not be rendered.
    /// </summary>
    /// <remarks>
    /// The specified environment names are compared case insensitively.
    /// </remarks>
    [Parameter]
    public string? Include { get; set; }

    /// <summary>
    /// Gets or sets a comma-separated list of environment names in which the content will not be rendered.
    /// </summary>
    /// <remarks>
    /// The specified environment names are compared case insensitively.
    /// </remarks>
    [Parameter]
    public string? Exclude { get; set; }

    /// <summary>
    /// Gets or sets the content to be rendered when the environment matches.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ShouldRenderContent())
        {
            builder.AddContent(0, ChildContent);
        }
    }

    private bool ShouldRenderContent()
    {
        var currentEnvironmentName = HostEnvironment.EnvironmentName?.Trim();

        if (string.IsNullOrEmpty(currentEnvironmentName))
        {
            // For consistency with MVC EnvironmentTagHelper, render content when environment name is not set
            // and no Include/Exclude are specified.
            if (string.IsNullOrWhiteSpace(Include) && string.IsNullOrWhiteSpace(Exclude))
            {
                return true;
            }

            return false;
        }

        // Check exclusions first - if current environment is excluded, don't render
        if (!string.IsNullOrWhiteSpace(Exclude))
        {
            foreach (var environment in ParseEnvironmentNames(Exclude))
            {
                if (environment.Equals(currentEnvironmentName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
        }

        // If no inclusions specified, render (unless excluded above)
        if (string.IsNullOrWhiteSpace(Include))
        {
            return true;
        }

        // Check if current environment is in the include list
        var hasEnvironments = false;
        foreach (var environment in ParseEnvironmentNames(Include))
        {
            hasEnvironments = true;
            if (environment.Equals(currentEnvironmentName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // If Include was specified but contained no valid environments, render content
        // (same behavior as when Include is not specified)
        if (!hasEnvironments)
        {
            return true;
        }

        return false;
    }

    private static IEnumerable<string> ParseEnvironmentNames(string names)
    {
        foreach (var segment in names.Split(NameSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = segment.Trim();
            if (trimmed.Length > 0)
            {
                yield return trimmed;
            }
        }
    }
}
