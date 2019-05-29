using System;
using System.Collections.Generic;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing.xunit
{
    /// <summary>
    /// Marks a test as "Flaky" so that the build will sequester it and ignore failures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute works by applying xUnit.net "Traits" based on the criteria specified in the attribute
    /// properties. Once these traits are applied, build scripts can include/exclude tests based on them.
    /// </para>
    /// <para>
    /// All flakiness-related traits start with <code>Flaky:</code> and are grouped first by the process running the tests: Azure Pipelines (AzP) or Helix.
    /// Then there is a segment specifying the "selector" which indicates where the test is flaky. Finally a segment specifying the value of that selector.
    /// The value of these traits is always either "true" or the trait is not present. We encode the entire selector in the name of the trait because xUnit.net only
    /// provides "==" and "!=" operators for traits, there is no way to check if a trait "contains" or "does not contain" a value. VSTest does support "contains" checks
    /// but does not appear to support "does not contain" checks. Using this pattern means we can use simple "==" and "!=" checks to either only run flaky tests, or exclude
    /// flaky tests.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [Fact]
    /// [Flaky("...", HelixQueues.Fedora28Amd64, AzurePipelines.macOS)]
    /// public void FlakyTest()
    /// {
    ///     // Flakiness
    /// }
    /// </code>
    ///
    /// <para>
    /// The above example generates the following facets:
    /// </para>
    ///
    /// <list type="bullet">
    /// <item>
    ///     <description><c>Flaky:Helix:Queue:Fedora.28.Amd64.Open</c> = <c>true</c></description>
    /// </item>
    /// <item>
    ///     <description><c>Flaky:AzP:OS:Darwin</c> = <c>true</c></description>
    /// </item>
    /// </list>
    ///
    /// <para>
    /// Given the above attribute, the Azure Pipelines macOS run can easily filter this test out by passing <c>-notrait "Flaky:AzP:OS:all=true" -notrait "Flaky:AzP:OS:Darwin=true"</c>
    /// to <c>xunit.console.exe</c>. Similarly, it can run only flaky tests using <c>-trait "Flaky:AzP:OS:all=true" -trait "Flaky:AzP:OS:Darwin=true"</c>
    /// </para>
    /// </example>
    [TraitDiscoverer("Microsoft.AspNetCore.Testing.xunit.FlakyTestDiscoverer", "Microsoft.AspNetCore.Testing")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public sealed class FlakyAttribute : Attribute, ITraitAttribute
    {
        /// <summary>
        /// Gets a URL to a GitHub issue tracking this flaky test.
        /// </summary>
        public string GitHubIssueUrl { get; }

        public IReadOnlyList<string> Filters { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FlakyAttribute"/> class with the specified <see cref="GitHubIssueUrl"/> and a list of <see cref="Filters"/>. If no
        /// filters are provided, the test is considered flaky in all environments.
        /// </summary>
        /// <remarks>
        /// At least one filter is required.
        /// </remarks>
        /// <param name="gitHubIssueUrl">The URL to a GitHub issue tracking this flaky test.</param>
        /// <param name="firstFilter">The first filter that indicates where the test is flaky. Use a value from <see cref="FlakyOn"/>.</param>
        /// <param name="additionalFilters">A list of additional filters that define where this test is flaky. Use values in <see cref="FlakyOn"/>.</param>
        public FlakyAttribute(string gitHubIssueUrl, string firstFilter, params string[] additionalFilters)
        {
            if(string.IsNullOrEmpty(gitHubIssueUrl))
            {
                throw new ArgumentNullException(nameof(gitHubIssueUrl));
            }

            if(string.IsNullOrEmpty(firstFilter))
            {
                throw new ArgumentNullException(nameof(firstFilter));
            }

            GitHubIssueUrl = gitHubIssueUrl;
            var filters = new List<string>();
            filters.Add(firstFilter);
            filters.AddRange(additionalFilters);
            Filters = filters;
        }
    }
}
