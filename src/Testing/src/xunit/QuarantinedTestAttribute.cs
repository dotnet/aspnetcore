using System;
using System.Collections.Generic;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Marks a test as "Quarantined" so that the build will sequester it and ignore failures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute works by applying xUnit.net "Traits" based on the criteria specified in the attribute
    /// properties. Once these traits are applied, build scripts can include/exclude tests based on them.
    /// </para>
    /// <para>
    /// All flakiness-related traits start with <c>Flaky:</c> and are grouped first by the process running the tests: Azure Pipelines (AzP) or Helix.
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
    /// [QuarantinedTest]
    /// public void FlakyTest()
    /// {
    ///     // Flakiness
    /// }
    /// </code>
    ///
    /// <para>
    /// The above example generates the following facet:
    /// </para>
    ///
    /// <list type="bullet">
    /// <item>
    ///     <description><c>Quarantined</c> = <c>true</c></description>
    /// </item>
    /// </list>
    /// </example>
    [TraitDiscoverer("Microsoft.AspNetCore.Testing." + nameof(QuarantinedTestTraitDiscoverer), "Microsoft.AspNetCore.Testing")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    public sealed class QuarantinedTestAttribute : Attribute, ITraitAttribute
    {
        /// <summary>
        /// Gets a URL to a GitHub issue tracking this quarantined test.
        /// </summary>
        public string GitHubIssueUrl { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QuarantinedTestAttribute"/> class with an optional <see cref="GitHubIssueUrl"/>.
        /// </summary>
        /// <param name="gitHubIssueUrl">A URL to a GitHub issue tracking this flaky test.</param>
        public QuarantinedTestAttribute(string gitHubIssueUrl = "")
        {
            GitHubIssueUrl = gitHubIssueUrl;
        }
    }
}
